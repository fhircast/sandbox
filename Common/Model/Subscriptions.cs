using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FHIRcastSandbox.Model
{


    public class Subscription
    {
        public static IEqualityComparer<Subscription> DefaultComparer => new SubscriptionComparer();

        [URLNameOverride("hub.callback")]
        public string Callback { get; set; }

        [URLNameOverride("hub.mode")]
        public SubscriptionMode? Mode { get; set; }

        [URLNameOverride("hub.topic")]
        public string Topic { get; set; }

        [URLNameOverride("hub.events")]
        public string[] Events { get; set; }

        [URLNameOverride("hub.secret")]
        public string Secret { get; set; }

        [URLNameOverride("hub.lease_seconds")]
        [FromForm(Name = "lease_seconds")]
        public int? LeaseSeconds { get; set; }

        [JsonIgnore]
        public TimeSpan? Lease => this.LeaseSeconds.HasValue ? TimeSpan.FromSeconds(this.LeaseSeconds.Value) : (TimeSpan?)null;

        [URLNameOverride("hub.challenge")]
        public string Challenge { get; set; }

        [URLNameOverride("hub.reason")]
        public string Reason { get; set; }

        public HubURL HubURL { get; set; }

        /// <summary>
        /// Gets a subscriber-unique ID of this subscription.
        /// </summary>
        /// <returns>An ID that is unique to the subscriber creating the subscription.</returns>
        public string GetUniqueId()
        {
            return GetSubscriptionId(this.Topic, this.Callback);
        }

        public static string GetSubscriptionId(string topic, string callback)
        {
            return BitConverter.ToString(SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes((topic ?? "") + (callback ?? ""))));
        }

        public bool IsInterestedInNotification(Notification notification)
        {
            return this.Events.Any(e => e == notification.Event.Event)
                && notification.Event.Topic == this.Topic;
        }

        public bool ValidModelState(SubscriptionModelStates state)
        {
            //State dependent validations

            //State independent validation
            if ((this.Callback == null) || (this.Mode == null) || (this.Topic == null) || (this.Events == null))
            {
                return false;
            }

            return true;
        }

        public static Subscription CreateNewSubscription(string subscriptionUrl, string topic, string[] events, string callback)
        {	        /// <summary>
            var rngCsp = new RNGCryptoServiceProvider();	        /// Gets a subscriber-unique ID of this subscription.
            var buffer = new byte[32];	        /// </summary>
            rngCsp.GetBytes(buffer);	        /// <returns>An ID that is unique to the subscriber creating the subscription.</returns>
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
    }

    public enum SubscriptionMode
    {
        subscribe,
        unsubscribe,
        denied,
    }

    public class SubscriptionComparer : IEqualityComparer<Subscription>
    {
        public bool Equals(Subscription sub1, Subscription sub2)
        {
            return sub1.Callback == sub2.Callback && sub1.Topic == sub2.Topic;
        }

        public int GetHashCode(Subscription subscription)
        {
            return subscription.Callback.GetHashCode() ^ subscription.Topic.GetHashCode();
        }
    }

    public class Notification : ModelBase
    {
        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "event")]
        public NotificationEvent Event { get; } = new NotificationEvent();
    }

    public class NotificationEvent
    {
        [ModelBinder(Name = "hub.topic")]
        [JsonProperty(PropertyName = "hub.topic")]
        public string Topic { get; set; }

        [ModelBinder(Name = "hub.event")]
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

    public class HubURL : ModelBase
    {
        public string URL { get; set; }

        public string[] HTTPHeaders { get; set; }
    }

    //https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Serialization_IContractResolver.htm
    public class SubscriptionContractResolver : DefaultContractResolver
    {
        private readonly SubscriptionModelStates _subscriptionModelStates;

        public SubscriptionContractResolver(SubscriptionModelStates use)
        {
            _subscriptionModelStates = use;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            IList<JsonProperty> updatedProperties = new List<JsonProperty>();

            foreach (JsonProperty prop in properties)
            {
                IList<Attribute> attributes = prop.AttributeProvider.GetAttributes(typeof(SubscriptionConditionallySerialize), false);
                foreach (SubscriptionConditionallySerialize att in attributes)
                {
                    if (att.ModelStates.Contains(_subscriptionModelStates))
                    {
                        updatedProperties.Add(prop);
                        break;
                    }
                }
            }

            return updatedProperties;
        }
    }

    #region Property Attribute

    public class SubscriptionConditionallySerialize : Attribute
    {
        public SubscriptionModelStates[] ModelStates { get; private set; }

        public SubscriptionConditionallySerialize(params SubscriptionModelStates[] modelStates)
        {
            this.ModelStates = modelStates;
        }
    }

    public enum SubscriptionModelStates
    {
        Request,
        Verification,
        ToClient
    }
    #endregion
}
