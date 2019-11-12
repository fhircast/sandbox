using Common.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FHIRcastSandbox.Rules
{
    public class HubSubscriptionCollection : ISubscriptions
    {
        private readonly ILogger<HubSubscriptionCollection> logger;

        private ConcurrentDictionary<string, SubscriptionRequest> pendedSubscriptions;
        private ConcurrentDictionary<string, SubscriptionRequest> activeSubscriptions;

        public HubSubscriptionCollection(ILogger<HubSubscriptionCollection> logger)
        {
            this.logger = logger;

            pendedSubscriptions = new ConcurrentDictionary<string, SubscriptionRequest>();
            activeSubscriptions = new ConcurrentDictionary<string, SubscriptionRequest>();
        }

        public ICollection<SubscriptionRequest> GetPendingSubscriptions()
        {
            return pendedSubscriptions.Values.ToList();
        }
        public ICollection<SubscriptionRequest> GetActiveSubscriptions()
        {
            return activeSubscriptions.Values.ToList();
        }

        /// <summary>
        /// Get a list of subscriptions based on the topic and event. Used to get which subscriptions to notify
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="notificationEvent"></param>
        /// <returns></returns>
        public ICollection<SubscriptionRequest> GetSubscriptions(string topic, string notificationEvent)
        {
            logger.LogDebug($"Finding subscriptions for topic: {topic} and event: {notificationEvent}");

            return activeSubscriptions
                .Select(x => x.Value)
                .Where(x => x.Topic == topic)
                .Where(x => x.Events.Contains(notificationEvent))
                .ToArray();
        }

        public void RemoveSubscription(SubscriptionRequest subscription)
        {
            logger.LogInformation($"Removing subscription {subscription}.");

            activeSubscriptions.TryRemove(subscription.CollectionKey, out SubscriptionRequest value);
        }

        /// <summary>
        /// Add a pending subscription. Store these before we validate it (webhook) or receive the websocket connection
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="key"></param>
        public void AddPendingSubscription(SubscriptionRequest subscription, string key)
        {
            pendedSubscriptions.AddOrUpdate(key, subscription, (k, o) => subscription);
        }

        /// <summary>
        /// Moves the pending subscription to the active subscription collection. Call this once the webhook subscription
        /// has been validated or we received the websocket connection.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ActivatePendedSubscription(string key)
        {
            return ActivatePendedSubscription(key, out SubscriptionRequest sub);
        }

        /// <summary>
        /// Moves the pending subscription to the active subscription collection. Call this once the webhook subscription
        /// has been validated or we received the websocket connection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public bool ActivatePendedSubscription(string key, out SubscriptionRequest subscription)
        {
            subscription = null;
            if (!pendedSubscriptions.Remove(key, out SubscriptionRequest pendedSub))
            {
                return false;
            }

            activeSubscriptions.AddOrUpdate(key, pendedSub, (k, o) => pendedSub);
            subscription = pendedSub;

            return true;
        }

        public bool UnsubscribeSubscription(string key)
        {
            //TODO: Implement unsubscribe
            return true;
        }

        
    }
}
