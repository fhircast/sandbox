using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace FHIRcastSandbox.Rules {
    public class Subscriptions : ISubscriptions {
        private readonly ILogger<Subscriptions> logger;
        private ImmutableList<Subscription> subscriptions = ImmutableList<Subscription>.Empty;

        public Subscriptions(ILogger<Subscriptions> logger) {
            this.logger = logger;
        }

        public ImmutableList<Subscription> GetActiveSubscriptions() {
            return this.subscriptions;
        }

        public void AddSubscription(Subscription subscription) {
            this.logger.LogInformation($"Adding subscription {subscription}.");
            this.subscriptions = this.subscriptions.Add(subscription);
        }

        public void RemoveSubscription(Subscription subscription) {
            this.logger.LogInformation($"Removing subscription {subscription}.");
            this.subscriptions = this.subscriptions.Remove(subscription, Subscription.DefaultComparer);
        }
    }
}
