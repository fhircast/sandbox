using Common.Model;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Hubs;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Hubs
{
    /// <summary>
    /// This is a SignalR hub for the js client, not to be confused with a FHIRcast hub.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.SignalR.Hub" />
    public class WebSubClientHub : Hub<IWebSubClient>
    {
        private readonly ILogger<WebSubClientHub> logger;
        private readonly Subscriptions _subscriptions;
        private readonly IConfiguration config;
        private readonly IHubContext<WebSubClientHub, IWebSubClient> webSubClientHubContext;

        private readonly InternalHubClient internalHubClient;

        public WebSubClientHub(ILogger<WebSubClientHub> logger, IConfiguration config, IHubContext<WebSubClientHub, IWebSubClient> hubContext, InternalHubClient internalHubClient, Subscriptions subscriptions)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config;
            webSubClientHubContext = hubContext;
            this.internalHubClient = internalHubClient;

            this.internalHubClient.SubscriberRemoved += InternalHubClient_SubscriberRemoved;
            this.internalHubClient.SubscriberAdded += InternalHubClient_SubscriberAdded;
            _subscriptions = subscriptions;
        }

        private async void InternalHubClient_SubscriberAdded(object sender, SubscriptionRequest subscription)
        {
            await AddSubscriber(Context.ConnectionId, subscription);
        }

        private async void InternalHubClient_SubscriberRemoved(object sender, SubscriptionRequest subscription)
        {
            await RemoveSubscriber(Context.ConnectionId, subscription);
        }

        /// <summary>
        /// Client is attempting to subscribe to another app
        /// </summary>
        /// <param name="subscriptionUrl"></param>
        /// <param name="topic"></param>
        /// <param name="events"></param>
        /// <param name="httpHeaders"></param>
        /// <returns></returns>
        public async Task Subscribe(string subscriptionUrl, string topic, string events, string[] httpHeaders)
        {
            if (string.IsNullOrEmpty(subscriptionUrl))
            {
                var hubBaseURL = config.GetValue("Settings:HubBaseURL", "localhost");
                var hubPort = config.GetValue("Settings:HubPort", 5000);
                subscriptionUrl = new UriBuilder("http", hubBaseURL, hubPort, "/api/hub").Uri.ToString();
            }

            string clientId = Context.ConnectionId;

            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[64];
            rngCsp.GetBytes(buffer);
            var secret = Convert.ToBase64String(buffer);
            var clientBaseURL = config.GetValue("Settings:ClientBaseURL", "localhost");
            var clientPort = config.GetValue("Settings:ClientPort", 5001);

            var callbackUri = new UriBuilder(
                "http",
                clientBaseURL,
                clientPort,
                $"/callback/{clientId}");

            SubscriptionRequest subscription = new SubscriptionRequest()
            {
                Callback = callbackUri.Uri.OriginalString,
                Mode = SubscriptionMode.subscribe,
                Topic = topic,
                Secret = secret,
                Events = events.Split(",", StringSplitOptions.RemoveEmptyEntries),
                Lease_Seconds = 3600,
                HubDetails = new HubDetails()
                {
                    HubUrl = subscriptionUrl,
                    HttpHeaders = httpHeaders
                }
            };

            if (!await PendAndPostSubscription(clientId, subscription))
            {
                // I don't know do something
            }
        }

        public async Task Unsubscribe(string topic)
        {
            string clientId = Context.ConnectionId;

            SubscriptionRequest subscription;
            if (!_subscriptions.GetClientSubscription(clientId, topic, out subscription))
            {
                return;
            }

            logger.LogDebug($"Unsubscribing subscription for {clientId}: {subscription}");

            subscription.Mode = SubscriptionMode.unsubscribe;

            if (!await PendAndPostSubscription(clientId, subscription))
            {
                // I don't know do something
                return;
            }
        }

        /// <summary>
        /// Posts the subscription request to its associated Hub. This is used for both new subscriptions
        /// as well as unsubscribing
        /// </summary>
        /// <param name="subscriptionRequest"></param>
        /// <returns>True if the Hub returned a success code, otherwise false</returns>
        private async Task<bool> PendAndPostSubscription(string clientId, SubscriptionRequest subscriptionRequest)
        {
            _subscriptions.AddPendingSubscription(clientId, subscriptionRequest);

            HttpClient client = new HttpClient();

            foreach (string header in subscriptionRequest.HubDetails.HttpHeaders)
            {
                string[] split = header.Split(":");
                client.DefaultRequestHeaders.Add(split[0], split[1]);
            }

            HttpResponseMessage response = await client.PostAsync(subscriptionRequest.HubDetails.HubUrl, subscriptionRequest.BuildPostHttpContent());

            if (!response.IsSuccessStatusCode)
            {
                _subscriptions.RemovePendingSubscription(clientId, subscriptionRequest);
            }
            //else
            //{
            //    await SubscriptionsChanged(clientId);
            //}

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Recieved an update from our client, send that notification to the hub (HubController -> Notify)
        /// for it to send out to the awaiting subscriber
        /// </summary>
        /// <param name="topic">topicId</param>
        /// <param name="eventName">event that occurred</param>
        /// <param name="model">contextual information sent down from client</param>
        /// <returns></returns>
        public async Task Update(string topic, string eventName, ClientModel model)
        {
            HttpClient httpClient = new HttpClient();

            // Build Notification object
            Notification notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
            };

            List<Hl7.Fhir.Model.Resource> resources = new List<Hl7.Fhir.Model.Resource>();

            Hl7.Fhir.Model.Patient patient = new Hl7.Fhir.Model.Patient();
            patient.Id = model.PatientID;
            if (patient.Id != "")
            {
                resources.Add(patient);
            }

            Hl7.Fhir.Model.ImagingStudy imagingStudy = new Hl7.Fhir.Model.ImagingStudy();
            imagingStudy.Id = model.AccessionNumber;    //This probably isn't exactly right, but is useful for testing
            //imagingStudy.Accession = new Hl7.Fhir.Model.Identifier("accession", model.AccessionNumber);
            if (imagingStudy.Id != "")
            {
                resources.Add(imagingStudy);
            }

            NotificationEvent notificationEvent = new NotificationEvent()
            {
                Topic = topic,
                Event = eventName,
                Context = resources.ToArray()
            };
            notification.Event = notificationEvent;

            // Build hub url to send notification to
            var hubBaseURL = this.config.GetValue("Settings:HubBaseURL", "localhost");
            var hubPort = this.config.GetValue("Settings:HubPort", 5000);
            string subscriptionUrl = new UriBuilder("http", hubBaseURL, hubPort, "/api/hub").Uri.ToString();

            // Send notification and await response
            logger.LogDebug($"Sending notification to {subscriptionUrl}/{topic}: {notification.ToString()}");
            var response = await httpClient.PostAsync($"{subscriptionUrl}/{topic}", new StringContent(notification.ToJson(), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        public string GetTopic()
        {
            logger.LogDebug($"Sending topic {this.Context.ConnectionId} up to client");
            internalHubClient.RegisterTopic(Context.ConnectionId);
            return Context.ConnectionId;
        }

        #region Calls To Client
        public async Task ReceivedNotification(string connectionId, Notification notification)
        {
            logger.LogDebug($"ReceivedNotification for {connectionId}: {notification.ToString()}");
            await webSubClientHubContext.Clients.Client(connectionId).ReceivedNotification(notification);
        }

        public async Task AddSubscriber(string connectionId, SubscriptionRequest subscription)
        {
            logger.LogDebug($"Adding subscriber for {connectionId}: {subscription.ToString()}");
            await webSubClientHubContext.Clients.Client(connectionId).SubscriberAdded(subscription);
        }

        public async Task RemoveSubscriber(string connectionId, SubscriptionRequest subscription)
        {
            logger.LogDebug($"Removing subscriber for {connectionId}: {subscription.ToString()}");
            await webSubClientHubContext.Clients.Client(connectionId).SubscriberRemoved(subscription);
        }

        public async Task SubscriptionsChanged(string clientId)
        {
            List<SubscriptionRequest> subscriptions = _subscriptions.ClientsSubscriptions(clientId);
            logger.LogDebug($"Subscriptions changed for {clientId}. New list: {subscriptions}");
            await webSubClientHubContext.Clients.Client(clientId).SubscriptionsChanged(_subscriptions.ClientsSubscriptions(clientId));
        }

        public async Task AlertMessage(string connectionId, string message)
        {
            logger.LogDebug($"Alerting {connectionId}: {message}");
            await webSubClientHubContext.Clients.Client(connectionId).AlertMessage(message);
        }
        #endregion
    }
}
