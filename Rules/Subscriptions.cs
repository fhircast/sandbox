using System.Collections.Immutable;
using FHIRcastSandbox.Model;

namespace FHIRcastSandbox.Rules {
    public class Subscriptions : ISubscriptions {
        private ImmutableList<Subscription> subscriptions = ImmutableList<Subscription>.Empty;

        public ImmutableList<Subscription> GetActiveSubscriptions() {
            return this.subscriptions;
        }

        public void AddSubscription(Subscription subscription) {
            this.subscriptions = this.subscriptions.Add(subscription);
        }

        public void RemoveSubscription(Subscription subscription) {
            this.subscriptions = this.subscriptions.Remove(subscription);
        }
    }
}
