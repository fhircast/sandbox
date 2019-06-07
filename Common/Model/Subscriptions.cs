using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace FHIRcastSandbox.Model {
    public abstract class SubscriptionBase : ModelBase {
        public static IEqualityComparer<Subscription> DefaultComparer => new SubscriptionComparer();

        [BindRequired]
        [URLNameOverride("hub.callback")]
        public string Callback { get; set; }

        [BindRequired]
        [URLNameOverride("hub.mode")]
        public SubscriptionMode? Mode { get; set; }

        [BindRequired]
        [URLNameOverride("hub.topic")]
        public string Topic { get; set; }

        [BindRequired]
        [URLNameOverride("hub.events")]
        [ModelBinder(typeof(EventsArrayModelBinder))]
        public string[] Events { get; set; }

        /// <summary>
        /// Gets a subscriber-unique ID of this subscription.
        /// </summary>
        /// <returns>An ID that is unique to the subscriber creating the subscription.</returns>
        public string GetUniqueId() {
            return GetSubscriptionId(this.Topic, this.Callback);
        }

        public static string GetSubscriptionId(string topic, string callback) {
            return BitConverter.ToString(SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes((topic ?? "") + (callback ?? ""))));
        }
    }

    public abstract class SubscriptionWithLease : SubscriptionBase {
        [URLNameOverride("hub.lease_seconds")]
        public int? Lease_Seconds { get; set; }

        [BindNever, JsonIgnore]
        public TimeSpan? Lease => this.Lease_Seconds.HasValue ? TimeSpan.FromSeconds(this.Lease_Seconds.Value) : (TimeSpan?)null;
    }

    public class Subscription : SubscriptionWithLease {
        [BindRequired]
        [URLNameOverride("hub.secret")]
        public string Secret { get; set; }

        [BindNever, JsonIgnore]
        public HubURL HubURL { get; set; }

        public static Subscription CreateNewSubscription(string subscriptionUrl, string topic, string[] events, string callback, int leaseSeconds = 3600) {
            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[32];
            rngCsp.GetBytes(buffer);
            var secret = BitConverter.ToString(buffer).Replace("-", "");
            var subscription = new Subscription()
            {
                Callback = callback,
                Events = events,
                Mode = SubscriptionMode.subscribe,
                Secret = secret,
                Lease_Seconds = leaseSeconds,
                Topic = topic
            };
            subscription.HubURL = new HubURL() { URL = subscriptionUrl };

            return subscription;
        }

        public bool IsInterestedInNotification(Notification notification) {
            return this.Events.Any(e => e == notification.Event.Event)
                && notification.Event.Topic == this.Topic;
        }
    }

    public class SubscriptionWithHubURL : Subscription
    {
        public SubscriptionWithHubURL(Subscription baseSubscription)
        {
            this.Callback = baseSubscription.Callback;
            this.Events = baseSubscription.Events;
            this.HubURL = baseSubscription.HubURL;
            this.Lease_Seconds = baseSubscription.Lease_Seconds;
            this.Mode = baseSubscription.Mode;
            this.Secret = baseSubscription.Secret;
            this.Topic = baseSubscription.Topic;
        }

        [BindNever]
        public HubURL HubURL { get; set; }
    }

    public class SubscriptionCancelled : SubscriptionBase {
        [URLNameOverride("hub.reason")]
        public string Reason { get; set; }
    }

    public class SubscriptionVerification : SubscriptionWithLease {
        [URLNameOverride("hub.challenge")]
        public string Challenge { get; set; }

        [URLNameOverride("hub.reason")]
        public string Reason { get; set; }
    }

    public enum SubscriptionMode {
        subscribe,
        unsubscribe,
        denied,
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
        public NotificationEvent Event { get; set; } = new NotificationEvent();
    }

    public class NotificationEvent {
        [ModelBinder(Name = "hub.topic")]
        [JsonProperty(PropertyName = "hub.topic")]
        public string Topic { get; set; }

        [ModelBinder(Name = "hub.event")]
        [JsonProperty(PropertyName = "hub.event")]
        public string Event { get; set; }

        [JsonProperty(PropertyName = "context")]
        public object[] Context { get; set; }
    }

    public class URLNameOverride : Attribute {
        public URLNameOverride(string value) {
            this.Value = value;
        }

        public string Value { get; set; }
    }

    public class HubURL : ModelBase
    {
        public string URL { get; set; }

        public string[] HTTPHeaders { get; set; }
    }
}
