using Common.Model;
using FHIRcastSandbox.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FHIRcastSandbox.WebSubClient.Rules
{
    public class Subscriptions
    {
        //Client's subscriptions
        private readonly ConcurrentDictionary<string, List<SubscriptionRequest>> _activeSubscriptions = new ConcurrentDictionary<string, List<SubscriptionRequest>>();
        private readonly ConcurrentDictionary<string, List<SubscriptionRequest>> _pendingSubscriptions = new ConcurrentDictionary<string, List<SubscriptionRequest>>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, SubscriptionRequest>> _subscriptionsToClient = new ConcurrentDictionary<string, ConcurrentDictionary<int, SubscriptionRequest>>();

        #region Client's Subscriptions
        /// <summary>
        /// This will be called by our client hub when we create a subscription request and post it to the external hub 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="subscription"></param>
        public void AddPendingSubscription(string clientId, SubscriptionRequest subscription)
        {
            ValidateClientIDInDictionary(clientId, _pendingSubscriptions);
            _pendingSubscriptions[clientId].Add(subscription);
        }

        public void RemovePendingSubscription(string clientId, SubscriptionRequest subscription)
        {
            ValidateClientIDInDictionary(clientId, _pendingSubscriptions);
            try
            {
                _pendingSubscriptions[clientId].Remove(subscription);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// This will be called by our callback controller when we receive a verification from the external app we subscribed to
        /// Occurs for new subscriptions, updating subscrtiptions, or unsubscribing. If unsubscribing then we will remove it from the dictionary.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="subscription"></param>
        /// <returns>
        ///     True if we had a matching pending subscription and can verify the subscription
        ///     False if we don't have a matching subscription and we shouldn't verify the subscription
        /// </returns>
        public bool VerifiedSubscription(string clientId, Common.Model.SubscriptionVerification subscription)
        {
            ValidateClientIDInDictionary(clientId, _pendingSubscriptions);
            ValidateClientIDInDictionary(clientId, _activeSubscriptions);

            SubscriptionRequest matchingRequest = new SubscriptionRequest();
            bool foundMatch = false;
            foreach (SubscriptionRequest item in _pendingSubscriptions[clientId])
            {
                if (item.Equals(subscription))
                {
                    matchingRequest = item;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                return false;
            }

            // If this same callback and topic exists then it will be overwritten in the external app so we should overwrite it here as well
            SubscriptionRequest activeSubscription;
            if (GetClientSubscription(clientId, subscription.Topic, out activeSubscription))
            {
                _activeSubscriptions[clientId].Remove(activeSubscription);
            }

            // Only add if we are subscribed, not if we are denied or unsubscribed
            if (subscription.Mode == Common.Model.SubscriptionMode.subscribe)
            {
                _activeSubscriptions[clientId].Add(matchingRequest);
            }

            return true;
        }

        public List<SubscriptionRequest> ClientsSubscriptions(string clientId)
        {
            ValidateClientIDInDictionary(clientId, _activeSubscriptions);
            return _activeSubscriptions[clientId];
        }

        /// <summary>
        /// A callback/topic combination defines a unique subscription. If a hub receives that same combo then it SHALL overwrite its 
        /// previous subscription. Therefore, since we use the clientId as the defining callback characteristic, we can find a unique
        /// subscription from just the clientId and topic.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="topic"></param>
        /// <param name="subscriptionRequest"></param>
        /// <returns></returns>    
        public bool GetClientSubscription(string clientId, string topic, out SubscriptionRequest subscriptionRequest)
        {
            ValidateClientIDInDictionary(clientId, _activeSubscriptions);
            List<SubscriptionRequest> listRequests = _activeSubscriptions[clientId];
            foreach (SubscriptionRequest subscription in listRequests)
            {
                // This assumes that we include our clientId in the callback (check WebSubClientHub)
                // Probably not a great long term assumption, but works for now.
                if (!subscription.Callback.Contains(clientId))
                {
                    continue;
                }

                if (!subscription.Topic.Equals(topic))
                {
                    continue;
                }

                subscriptionRequest = subscription;
                return true;
            }

            subscriptionRequest = null;
            return false;
        }

        public bool HasMatchingSubscription(string clientId, Notification notification)
        {
            ValidateClientIDInDictionary(clientId, _activeSubscriptions);
            List<SubscriptionRequest> listRequests = _activeSubscriptions[clientId];
            foreach (SubscriptionRequest subscription in listRequests)
            {
                if (SubscriptionMatchesNotification(subscription, notification))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Subscriptions To Client
        public void AddSubscriptionToClient(string clientId, SubscriptionRequest subscription)
        {
            //TODO
        }

        public void RemoveSubscriptionFromClient(string clientId, SubscriptionRequest subscription)
        {
            //TODO
        }

        public List<SubscriptionRequest> SubscribersToNotify(string clientId, Notification notification)
        {
            if (!_subscriptionsToClient.ContainsKey(clientId))
            {
                return new List<SubscriptionRequest>();
            }

            ConcurrentDictionary<int, SubscriptionRequest> clientSubscribers = _subscriptionsToClient[clientId];
            return clientSubscribers.Where(x => SubscriptionMatchesNotification(x.Value, notification)).Select(x => x.Value).ToList();
        }
        #endregion

        #region Private Methods
        private bool SubscriptionMatchesNotification(SubscriptionRequest subscription, Notification notification)
        {
            return (subscription.Topic.Equals(notification.Event.Topic) && subscription.Events.Contains(notification.Event.Event));
        }

        private void ValidateClientIDInDictionary(string clientId, ConcurrentDictionary<string, List<SubscriptionRequest>> dictionary)
        {
            if (!dictionary.ContainsKey(clientId))
            {
                List<SubscriptionRequest> subscriptionRequests = new List<SubscriptionRequest>();
                dictionary.AddOrUpdate(clientId, subscriptionRequests, (key, oldValue) => subscriptionRequests);
            }
        }
        #endregion
    }
}
