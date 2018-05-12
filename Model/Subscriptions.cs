using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Model {
    public abstract class SubscriptionBase : ModelBase {
        public static IEqualityComparer<Subscription> DefaultComparer => new SubscriptionComparer();

        [BindRequired]
        public string UID { get; set; }

        [BindRequired]
        public Uri Callback { get; set; }

        [BindRequired]
        public SubscriptionMode? Mode { get; set; }

        [BindRequired]
        public string Topic { get; set; }

        [BindRequired]
        public string[] Events { get; set; }
    }

    public abstract class SubscriptionWithLease : SubscriptionBase {
        [ModelBinder(Name = "lease_seconds")]
        public int? LeaseSeconds { get; set; }

        [BindNever, JsonIgnore]
        public TimeSpan? Lease => this.LeaseSeconds.HasValue ? TimeSpan.FromSeconds(this.LeaseSeconds.Value) : (TimeSpan?)null;
    }

    public class Subscription : SubscriptionWithLease {
        [BindRequired]
        public string Secret { get; set; }
        [BindNever, JsonIgnore]
        public string HubURL { get; set; }

        public void LogSubscriptionInfo(ILogger logger, string context)
        {
            logger.LogDebug($"Subscription for {context}: \n" +
                $"\t Callback: {this.Callback} \n" +
                $"\t Mode: {this.Mode} \n" +
                $"\t Topic: {this.Topic} \n" +
                $"\t Secret: {this.Secret} \n" +
                $"\t Events: {string.Join(",", this.Events)} \n" +
                $"\t Lease: {this.LeaseSeconds} \n" +
                $"\t UID: {this.UID}"
                );
        }
    }

    public class SubscriptionCancelled : SubscriptionBase {
        public string Reason { get; set; }
    }

    public class SubscriptionVerification : SubscriptionWithLease {
        public string Challenge { get; set; }
    }

    public enum SubscriptionMode {
        subscribe,
        unsubscribe,
    }

    public class SubscriptionComparer : IEqualityComparer<Subscription> {
        public bool Equals(Subscription sub1, Subscription sub2) {
            return sub1.Callback == sub2.Callback && sub1.Topic == sub2.Topic;
        }

        public int GetHashCode(Subscription subscription) {
            return subscription.Callback.GetHashCode() ^ subscription.Topic.GetHashCode();
        }
    }

    public class Notification : ModelBase {
        public DateTime Timestamp { get; set; }
        public string Id { get; set; }
        public NotificationEvent Event { get; } = new NotificationEvent();
    }

    public class NotificationEvent {
        public string Topic { get; set; }
        public string Event { get; set; }
        public object[] Context { get; set; }
    }
}
