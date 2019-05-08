using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        [FromForm (Name = "lease_seconds")]
        public int? LeaseSeconds { get; set; }

        [BindNever, JsonIgnore]
        public TimeSpan? Lease => this.LeaseSeconds.HasValue ? TimeSpan.FromSeconds(this.LeaseSeconds.Value) : (TimeSpan?)null;
    }

    public class Subscription : SubscriptionWithLease {
        [BindRequired]
        [URLNameOverride("hub.secret")]
        public string Secret { get; set; }

        [BindNever, JsonIgnore]
        [JSONSerializationAttribue(SubscriptionObjectUses.Request)]
        public HubURL HubURL { get; set; }

        public static Subscription CreateNewSubscription(string subscriptionUrl, string topic, string[] events, string callback) {
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
                LeaseSeconds = 3600,
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
            this.LeaseSeconds = baseSubscription.LeaseSeconds;
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
        public NotificationEvent Event { get; } = new NotificationEvent();
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

    public class DynamicContractResolver : DefaultContractResolver
    {
        private readonly SubscriptionObjectUses _subscriptionObjectUse;

        public DynamicContractResolver(SubscriptionObjectUses use)
        {
            _subscriptionObjectUse = use;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            IList<JsonProperty> updatedProperties = new List<JsonProperty>();

            foreach (JsonProperty prop in properties)
            {
                IList<Attribute> attributes = prop.AttributeProvider.GetAttributes(typeof(JSONSerializationAttribue), false);
                foreach (JSONSerializationAttribue att in attributes)
                {
                    if (att.SubscriptionUses.Contains(_subscriptionObjectUse))
                    {
                        updatedProperties.Add(prop);
                        break;
                    }
                }
            }

            properties = updatedProperties;

            // only serializer properties that start with the specified character
            //properties =
            //    properties.Where(p => p.PropertyName.StartsWith(_startingWithChar.ToString())).ToList();
            //properties = properties.Where(p => p.AttributeProvider.GetAttributes(typeof(JSONSerializationAttribue,false).))
            //properties = properties.Where(p => p.AttributeProvider.GetAttributes<JSONSerializationAttribue>(typeof(JSONSerializationAttribue), false).Select(x =>  //.ToList<JSONSerializationAttribue>().Where(x => x.SubscriptionUses.Contains(_subscriptionObjectUse));

            return properties;
        }
    }

    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public new static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(Subscription))
            {
                property.ShouldSerialize =
                    instance =>
                    {
                        // Employee e = (Employee)instance;
                        return true; //e.Manager != e;
                    };
            }

            return property;
        }
    }

    #region Property Attribute
    public class JSONSerializationAttribue : Attribute
    {
        public JSONSerializationAttribue(params SubscriptionObjectUses[] subscriptionObjectUses)
        {
            this.SubscriptionUses = subscriptionObjectUses;
        }

        public SubscriptionObjectUses[] SubscriptionUses { get; private set; }
    }

    public enum SubscriptionObjectUses
    {
        Request,
        Verification,
        ToClient
    }
    #endregion
}
