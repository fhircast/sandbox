using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Rules {
    public class HubSubscriptionCollection : ISubscriptions {
        private readonly ILogger<HubSubscriptionCollection> logger;
        private ImmutableHashSet<Subscription> subscriptions = ImmutableHashSet<Subscription>.Empty.WithComparer(Subscription.DefaultComparer);

        public HubSubscriptionCollection(ILogger<HubSubscriptionCollection> logger) {
            this.logger = logger;
        }

        public ICollection<Subscription> GetActiveSubscriptions() {
            return this.subscriptions;
        }

        public ICollection<Subscription> GetSubscriptions(string topic, string notificationEvent) {
            this.logger.LogDebug($"Finding subscriptions for topic: {topic} and event: {notificationEvent}");
            return this.subscriptions
                .Where(x => x.Topic == topic)
                .Where(x => x.Events.Contains(notificationEvent))
                .ToArray();
        }

        public Subscription GetSubscription(string topic)
        {
            return this.subscriptions.Where(x => x.Topic == topic).First();
        }

        public void AddSubscription(Subscription subscription) {
            this.logger.LogInformation($"Adding subscription {subscription}.");
            this.subscriptions = this.subscriptions.Add(subscription);
        }

        public void RemoveSubscription(Subscription subscription) {
            this.logger.LogInformation($"Removing subscription {subscription}.");
            this.subscriptions = this.subscriptions.Remove(subscription);
        }
    }
}
