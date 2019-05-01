using FHIRcastSandbox.Model;
using System.Collections.Concurrent;
using System.Linq;
using System;
using System.Collections.Generic;

namespace FHIRcastSandbox.WebSubClient.Rules {
    //TODO: Can probably clean up the for loops using lambda expressions, but I couldn't figure it out beyond where it is so have at it

    public class ClientSubscriptions {
        //a connection can have multiple subscriptions so need the inner dictionary as well.
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)>> subscriptions = 
            new ConcurrentDictionary<string, ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)>>();

        public void AddPendingSubscription(string connectionId, Subscription subscription) {
            if (connectionId == null) {
                throw new ArgumentNullException(nameof(connectionId));
            }

            ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)> subInfo = new ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)>();
            subInfo.AddOrUpdate(subscription.Topic, (new SubscriptionInfo { Status = SubscriptionStatus.Pending }, subscription), (conId, value) => value);

            
            this.subscriptions.AddOrUpdate(connectionId, subInfo, (key, oldDict) =>
            {
                foreach (KeyValuePair<string, (SubscriptionInfo info, Subscription sub)> kvp in subInfo)
                {
                    oldDict.AddOrUpdate(kvp.Key, kvp.Value, (topic, oldVal) => kvp.Value);
                }
                return oldDict;
            });
        }

        public void ActivateSubscription(string subscriptionId) {
            List<(SubscriptionInfo, Subscription)> subs = new List<(SubscriptionInfo, Subscription)>();
            foreach (KeyValuePair<string, ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)>> kvp in this.subscriptions)
            {
                subs.AddRange(kvp.Value.Where(x => x.Value.info.Status == SubscriptionStatus.Pending)
                                        .Where(x => x.Value.sub.Topic == subscriptionId).Select(x => x.Value));
            }

            foreach ((SubscriptionInfo, Subscription) sub in subs)
            {
                sub.Item1.Status = SubscriptionStatus.Active;
            }          
        }

        public string[] GetSubscribedClients(Notification notification) {
            List<string> clients = new List<string>();
            foreach (KeyValuePair<string, ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)>> kvp in this.subscriptions)
            {
                clients.AddRange(kvp.Value.Where(x => x.Value.sub.IsInterestedInNotification(notification)).Select(x => kvp.Key));

                
            }
            return clients.ToArray();
        }

        public Subscription GetSubscription(string subscriptionId, string topic) {
            (_, var subscription) = this.GetExistingSubscription(subscriptionId, topic);
            return subscription;
        }

        public SubscriptionVerificationValidation ValidateVerification(string clientConnectionId, SubscriptionVerification verification) {
            var existingSubscription = this.GetExistingSubscription(clientConnectionId, verification.Topic);

            if (existingSubscription.Equals(default((SubscriptionInfo, Subscription)))) {
                return SubscriptionVerificationValidation.DoesNotExist;
            }
            if (existingSubscription.info.Status == SubscriptionStatus.Active) {
                return SubscriptionVerificationValidation.IsAlreadyActive;
            }
            return SubscriptionVerificationValidation.IsPendingVerification;
        }

        public void RemoveSubscription(string clientConnectionId) {
            this.subscriptions.TryRemove(clientConnectionId, out _);
        }

        public void RemoveSubscription(string clientConnectionId, string topic)
        {
            this.subscriptions[clientConnectionId].TryRemove(topic, out _);
        }

        private (SubscriptionInfo info, Subscription subscription) GetExistingSubscription(string clientConnectionId, string topic) {
            return this.subscriptions[clientConnectionId][topic];
        }
    }

    public class SubscriptionInfo {
        public SubscriptionStatus Status { get; set; }
    }

    public enum SubscriptionStatus { Pending, Active }

    public enum SubscriptionVerificationValidation { IsPendingVerification, DoesNotExist, IsAlreadyActive }
}
