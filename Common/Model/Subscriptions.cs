using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Model {
    public abstract class SubscriptionBase : ModelBase {
        public static IEqualityComparer<Subscription> DefaultComparer => new SubscriptionComparer();

        [BindNever]
        public string UID { get; set; }

        [BindRequired]
        [URLNameOverride("hub.callback")]
        public Uri Callback { get; set; }

        [BindRequired]
        [URLNameOverride("hub.mode")]
        public SubscriptionMode? Mode { get; set; }

        [BindRequired]
        [URLNameOverride("hub.topic")]
        public string Topic { get; set; }

        [BindRequired]
        [URLNameOverride("hub.events")]
        public string[] Events { get; set; }
    }

    public abstract class SubscriptionWithLease : SubscriptionBase {
        [URLNameOverride("hub.lease_seconds")]
        public int? LeaseSeconds { get; set; }

        [BindNever, JsonIgnore]
        public TimeSpan? Lease => this.LeaseSeconds.HasValue ? TimeSpan.FromSeconds(this.LeaseSeconds.Value) : (TimeSpan?)null;
    }

    public class Subscription : SubscriptionWithLease {
        [BindRequired]
        [URLNameOverride("hub.secret")]
        public string Secret { get; set; }
        [BindNever, JsonIgnore]
        public string HubURL { get; set; }
    }

    public class SubscriptionCancelled : SubscriptionBase {
        [URLNameOverride("hub.reason")]
        public string Reason { get; set; }
    }

    public class SubscriptionVerification : SubscriptionWithLease {
        [URLNameOverride("hub.challenge")]
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
        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "event")]
        public NotificationEvent Event { get; } = new NotificationEvent();
    }

    public class NotificationEvent {
        [JsonProperty(PropertyName = "hub.topic")]
        public string Topic { get; set; }
        [JsonProperty(PropertyName = "hub.event")]
        public string Event { get; set; }
        [JsonProperty(PropertyName = "context")]
        public object[] Context { get; set; }
    }

    public class URLNameOverride : Attribute
    {
        public URLNameOverride(string value)
        {
            this.Value = value;
        }

        public string Value { get; set; }
    }
}
