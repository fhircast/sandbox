using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using FHIRcastSandbox.WebSubClient.Hubs;

namespace FHIRcastSandbox.Hubs
{
    /// <summary>
    /// This is a SignalR hub for the js client, not to be confused with a FHIRcast hub.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.SignalR.Hub" />
    public class WebSubClientHub : Hub<IWebSubClient> {
        private readonly ILogger<WebSubClientHub> logger;
        private readonly ClientSubscriptions clientSubscriptions;
        private readonly IHubSubscriptions hubSubscriptions;
        private readonly IConfiguration config;
        private readonly IHubContext<WebSubClientHub, IWebSubClient> webSubClientHubContext;

        private readonly InternalHubClient internalHubClient;
     
        public WebSubClientHub(ILogger<WebSubClientHub> logger, ClientSubscriptions clientSubscriptions, IHubSubscriptions hubSubscriptions, IConfiguration config, IHubContext<WebSubClientHub, IWebSubClient> hubContext, InternalHubClient internalHubClient) {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientSubscriptions = clientSubscriptions ?? throw new ArgumentNullException(nameof(clientSubscriptions));
            this.hubSubscriptions = hubSubscriptions ?? throw new ArgumentNullException(nameof(hubSubscriptions));
            this.config = config;
            webSubClientHubContext = hubContext;
            this.internalHubClient = internalHubClient;

            this.internalHubClient.SubscriberAdded += InternalHubClient_SubscriberAdded;
        }

        private void InternalHubClient_SubscriberAdded(object sender, Subscription subscription)
        {
            AddSubscriber(Context.ConnectionId, subscription);
        }

        public async Task Subscribe(string subscriptionUrl, string topic, string events, string[] httpHeaders) {
            if (string.IsNullOrEmpty(subscriptionUrl)) {
                var hubBaseURL = this.config.GetValue("Settings:HubBaseURL", "localhost");
                var hubPort = this.config.GetValue("Settings:HubPort", 5000);
                subscriptionUrl = new UriBuilder("http", hubBaseURL, hubPort, "/api/hub").Uri.ToString();
            }

            var connectionId = this.Context.ConnectionId;

            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[64];
            rngCsp.GetBytes(buffer);
            var secret = Convert.ToBase64String(buffer);
            var clientBaseURL = this.config.GetValue("Settings:ClientBaseURL", "localhost");
            var clientPort = this.config.GetValue("Settings:ClientPort", 5001);

            var callbackUri = new UriBuilder(
                "http",
                clientBaseURL,
                clientPort,
                $"/callback/{connectionId}");

            var subscription = new Subscription()
            {
                Callback = callbackUri.Uri.OriginalString,
                Events = events.Split(",", StringSplitOptions.RemoveEmptyEntries),
                Mode = SubscriptionMode.subscribe,
                Secret = secret,
                Lease_Seconds = 3600,
                Topic = topic,
                HubURL = new HubURL() { URL = subscriptionUrl, HTTPHeaders = httpHeaders }
            };

            // First adding to pending and then sending the subscription to
            // prevent a race.
            this.clientSubscriptions.AddPendingSubscription(connectionId, subscription);
            try {
                await this.hubSubscriptions.SubscribeAsync(subscription);
            }
            catch {
                this.clientSubscriptions.RemoveSubscription(connectionId);
                throw;
            }
        }

        public async Task Unsubscribe(string topic) {
            var clientConnectionId = this.Context.ConnectionId;
            this.logger.LogDebug($"Unsubscribing subscription {clientConnectionId}");
            Subscription sub = this.clientSubscriptions.GetSubscription(clientConnectionId, topic);
            sub.Mode = SubscriptionMode.unsubscribe;

            this.clientSubscriptions.PendingRemovalSubscription(clientConnectionId, topic);
            await this.hubSubscriptions.Unsubscribe(sub);

            //Remove subscription
            //TODO: implement this using RemoveSubscription
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
            this.logger.LogDebug($"Sending notification to {subscriptionUrl}/{topic}: {notification.ToString()}");
            var response = await httpClient.PostAsync($"{subscriptionUrl}/{topic}", new StringContent(notification.ToJson(), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        public string GetTopic()
        {
            this.logger.LogDebug($"Sending topic {this.Context.ConnectionId} up to client");
            internalHubClient.RegisterTopic(Context.ConnectionId);
            return this.Context.ConnectionId;
        }

        #region Calls To Client
        public async Task ReceivedNotification(string connectionId, Notification notification)
        {
            logger.LogDebug($"ReceivedNotification for {connectionId}: {notification.ToString()}");
            await webSubClientHubContext.Clients.Client(connectionId).ReceivedNotification(notification);
        }

        public async Task AddSubscription(string connectionId, SubscriptionWithHubURL subscription)
        {
            logger.LogDebug($"Adding subscription for {connectionId}: {subscription.ToString()}");
            await webSubClientHubContext.Clients.Client(connectionId).AddSubscription(subscription);
        }

        public async Task AddSubscriber(string connectionId, Subscription subscription)
        {
            logger.LogDebug($"Adding subscriber for {connectionId}: {subscription.ToString()}");
            await webSubClientHubContext.Clients.Client(connectionId).AddSubscriber(subscription);
        }

        public async Task AlertMessage(string connectionId, string message)
        {
            logger.LogDebug($"Alerting {connectionId}: {message}");
            await webSubClientHubContext.Clients.Client(connectionId).AlertMessage(message);
        }
        #endregion
    }
}
