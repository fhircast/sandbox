using FHIRcastSandbox.Model;
using System.Collections.Concurrent;
using System.Linq;
using System;

namespace FHIRcastSandbox.WebSubClient.Rules {
    public class ClientSubscriptions {
        private readonly ConcurrentDictionary<string, (SubscriptionInfo info, Subscription sub)> subscriptions =
            new ConcurrentDictionary<string, (SubscriptionInfo, Subscription)>();

        public void AddPendingSubscription(string connectionId, Subscription subscription) {
            if (connectionId == null) {
                throw new ArgumentNullException(nameof(connectionId));
            }

            this.subscriptions.AddOrUpdate(connectionId, (new SubscriptionInfo { Status = SubscriptionStatus.Pending }, subscription), (conId, value) => value);
        }

        public void ActivateSubscription(string subscriptionId) {
            var subscriptionsToActivate = this.subscriptions
                .Where(x => x.Value.info.Status == SubscriptionStatus.Pending)
                .Where(x => x.Value.sub.Topic == subscriptionId);

            foreach (var sub in subscriptionsToActivate) {
                sub.Value.info.Status = SubscriptionStatus.Active;
            }
        }

        public string[] GetSubscribedClients(Notification notification) {
            return this.subscriptions
                .Where(x => x.Value.info.Status == SubscriptionStatus.Active)
                .Where(x => x.Value.sub.IsInterestedInNotification(notification))
                .Select(x => x.Key)
                .ToArray();
        }

        public Subscription GetSubscription(string subscriptionId) {
            (_, var subscription) = this.GetExistingSubscription(subscriptionId);
            return subscription;
        }

        public SubscriptionVerificationValidation ValidateVerification(string clientConnectionId, SubscriptionVerification verification) {
            var existingSubscription = this.GetExistingSubscription(clientConnectionId);

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

        private (SubscriptionInfo info, Subscription subscription) GetExistingSubscription(string clientConnectionId) {
            return this.subscriptions[clientConnectionId];
        }
    }

    public class SubscriptionInfo {
        public SubscriptionStatus Status { get; set; }
    }

    public enum SubscriptionStatus { Pending, Active }

    public enum SubscriptionVerificationValidation { IsPendingVerification, DoesNotExist, IsAlreadyActive }
}
