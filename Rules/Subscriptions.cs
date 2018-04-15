using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace FHIRcastSandbox.Rules {
    public class Subscriptions : ISubscriptions {
        private readonly ILogger<Subscriptions> logger;
        private ImmutableHashSet<Subscription> subscriptions = ImmutableHashSet<Subscription>.Empty.WithComparer(Subscription.DefaultComparer);

        public Subscriptions(ILogger<Subscriptions> logger) {
            this.logger = logger;
        }

        public ICollection<Subscription> GetActiveSubscriptions() {
            return this.subscriptions;
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
