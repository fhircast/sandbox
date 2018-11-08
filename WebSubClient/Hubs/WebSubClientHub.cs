using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using System.Net;

namespace FHIRcastSandbox.Hubs {
    /// <summary>
    /// This is a SignalR hub, not ot be confused with a FHIRcast hub.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.SignalR.Hub" />
    public class WebSubClientHub : Hub {
        private readonly ILogger<WebSubClientHub> logger;
        private readonly ClientSubscriptions clientSubscriptions;
        private readonly IHubSubscriptions hubSubscriptions;

        public WebSubClientHub(ILogger<WebSubClientHub> logger, ClientSubscriptions clientSubscriptions, IHubSubscriptions hubSubscriptions) {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientSubscriptions = clientSubscriptions ?? throw new ArgumentNullException(nameof(clientSubscriptions));
            this.hubSubscriptions = hubSubscriptions ?? throw new ArgumentNullException(nameof(hubSubscriptions));
        }

        public async Task Subscribe(string subscriptionUrl, string topic, string events) {
            if (string.IsNullOrEmpty(subscriptionUrl)) {
                subscriptionUrl = new UriBuilder("http", "localhost", 5000, "/api/hub").Uri.ToString();
            }

            var connectionId = this.Context.ConnectionId;

            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[64];
            rngCsp.GetBytes(buffer);
            var secret = Convert.ToBase64String(buffer);
            var callbackUri = new UriBuilder(
                "http",
                "localhost",
                5001,
                $"/callback/{connectionId}");

            var subscription = new Subscription()
            {
                Callback = callbackUri.Uri.OriginalString,
                Events = events.Split(",", StringSplitOptions.RemoveEmptyEntries),
                Mode = SubscriptionMode.subscribe,
                Secret = secret,
                LeaseSeconds = 3600,
                Topic = topic,
                HubURL = subscriptionUrl,
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

        public async Task Unsubscribe() {
            var clientConnectionId = this.Context.ConnectionId;
            this.logger.LogDebug($"Unsubscribing subscription {clientConnectionId}");
            Subscription sub = this.clientSubscriptions.GetSubscription(clientConnectionId);
            sub.Mode = SubscriptionMode.unsubscribe;

            await this.hubSubscriptions.Unsubscribe(sub);
            this.clientSubscriptions.RemoveSubscription(clientConnectionId);

        }
    }
}
