using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FHIRcastSandbox.WebSubClient.Hubs
{
    /// <summary>
    /// Client object in InternalHub SignalR communcation. Handles talking to and receiving messages from the Hub project.
    /// These messages include new subscriptions to this client (topic) or this client sending out a notification to a 
    /// subscriber.
    /// </summary>
    public class InternalHubClient
    {
        #region Member Variables
        private readonly ILogger<InternalHubClient> logger;
        private readonly IConfiguration config;
        private HubConnection hubConnection; 
        #endregion

        #region Events
        public delegate void SubscriberAddedEventHandler(object sender, Subscription subscription);

        /// <summary>
        /// Event used to inform the WebSubClientHub of a new subscriber since it has the connection to the js client.
        /// Couldn't get a direct reference to WebSubClientHub because it created a circular dependency.
        /// </summary>
        public event SubscriberAddedEventHandler SubscriberAdded;
        public event EventHandler<string> Error;

        private void RaiseError(string errorMessage)
        {
            logger.LogError(errorMessage);
            EventHandler<string> handler = Error;
            handler?.Invoke(this, errorMessage);
        }

        private void RaiseAddSubscriber(Subscription subscription)
        {
            logger.LogDebug($"Received subscriber notification from internal hub: {subscription.ToString()}");
            SubscriberAddedEventHandler handler = SubscriberAdded;
            handler?.Invoke(this, subscription);
        }
        #endregion

        public InternalHubClient(ILogger<InternalHubClient> logger, IConfiguration config)
        {
            this.logger = logger;
            this.config = config;

            CreateInternalHubConnection();
        }

        /// <summary>
        /// Initiate SignalR connection with InternalHub
        /// </summary>
        private async void CreateInternalHubConnection()
        {
            string hubBaseURL = config.GetValue("Settings:HubBaseURL", "localhost");
            int hubPort = config.GetValue("Settings:HubPort", 5000);

            hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{hubBaseURL}:{hubPort}/internalhub")
                .Build();

            // Believe this automatically tries to reconnect. 
            // <a href="https://docs.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-2.2">ASP.NET Core SignalR documentation</a>
            hubConnection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await hubConnection.StartAsync();
            };

            // Add method handlers
            hubConnection.On<Subscription>("AddSubscriber", AddSubscriber);
            //hubConnection.On<Subscription>("RemoveSubscriber", RemoveSubscriber); //TODO: Implement this interaction

            try
            {
                await hubConnection.StartAsync();
                logger.LogDebug("Started connection to internalhub");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error connecting to InternalHub: {ex.Message}";
                logger.LogError(errorMessage);
                RaiseError(errorMessage);
            }
        }
        

        #region Calls from WebSubHub
        public async void RegisterTopic(string topic)
        {
            CreateInternalHubConnection();
            await RegisterTopicInternal(topic);
        }
        #endregion

        #region Calls from Internal Hub
        private void AddSubscriber(Subscription subscription)
        {
            RaiseAddSubscriber(subscription);
        }  
        #endregion

        #region Calls to Internal Hub
        private async Task RegisterTopicInternal(string topic)
        {
            logger.LogDebug($"Registering {topic} with internal hub");
            await hubConnection.InvokeAsync("RegisterTopic", topic);
        }
        #endregion

    }
}
