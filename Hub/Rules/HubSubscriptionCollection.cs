using Common.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FHIRcastSandbox.Rules
{
    public class HubSubscriptionCollection : ISubscriptions
    {
        private readonly ILogger<HubSubscriptionCollection> logger;
        private ImmutableHashSet<SubscriptionRequest> subscriptions = ImmutableHashSet<SubscriptionRequest>.Empty;

        public HubSubscriptionCollection(ILogger<HubSubscriptionCollection> logger)
        {
            this.logger = logger;
        }

        public ICollection<SubscriptionRequest> GetActiveSubscriptions()
        {
            return this.subscriptions;
        }

        public ICollection<SubscriptionRequest> GetSubscriptions(string topic, string notificationEvent)
        {
            this.logger.LogDebug($"Finding subscriptions for topic: {topic} and event: {notificationEvent}");
            return this.subscriptions
                .Where(x => x.Topic == topic)
                .Where(x => x.Events.Contains(notificationEvent))
                .ToArray();
        }

        public SubscriptionRequest GetSubscription(string topic)
        {
            return this.subscriptions.Where(x => x.Topic == topic).First();
        }

        public void AddSubscription(SubscriptionRequest subscription)
        {
            this.logger.LogInformation($"Adding subscription {subscription}.");
            this.subscriptions = this.subscriptions.Add(subscription);
        }

        public void RemoveSubscription(SubscriptionRequest subscription)
        {
            this.logger.LogInformation($"Removing subscription {subscription}.");
            this.subscriptions = this.subscriptions.Remove(subscription);
        }
    }
}
