using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web;

namespace Common.Model
{
    /// <summary>
    /// Base class that contains the properties used by all Subscription interactions
    /// </summary>
    public abstract class SubscriptionBase : ModelBase
    {
        #region Properties
        [BindRequired]
        public SubscriptionMode Mode { get; set; }

        [BindRequired]
        public string Topic { get; set; }

        [BindRequired]
        [ModelBinder(typeof(EventsArrayModelBinder))]
        public string[] Events { get; set; }

        public int Lease_Seconds { get; set; } 
        #endregion

        #region Overrides
        /// <summary>
        /// Tests for equality based on the topic and the events subscribed to
        /// </summary>
        /// <param name="obj">Object comparing to</param>
        /// <returns>True if the objects are equal</returns>
        public override bool Equals(object obj)
        {
            if ((obj == null) || (!(obj is SubscriptionBase)))
            {
                return false;
            }
            else
            {
                // Compare topic and the events (order doesn't matter here)
                SubscriptionBase that = (SubscriptionBase)obj;
                return Topic.Equals(that.Topic) && new HashSet<string>(Events).SetEquals(that.Events);
            }
        }

        /// <summary>
        /// Not really used, but added for completeness
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = -50061919;
            hashCode = hashCode * -1521134295 + Mode.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Topic);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Events);
            hashCode = hashCode * -1521134295 + Lease_Seconds.GetHashCode();
            return hashCode;
        } 
        #endregion
    }

    /// <summary>
    /// Represents a subscription request. This can be for a new/updated subscription request
    /// or to unsubscribe an existing subscription.
    /// </summary>
    public class SubscriptionRequest : SubscriptionBase
    {
        #region Properties
        [BindRequired]
        public string Callback { get; set; }

        [BindRequired]
        public string Secret { get; set; }

        public SubscriptionChannelType? ChannelType { get; set; }

        [BindNever]
        public HubDetails HubDetails { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// This builds the HTTP content used in subscription requests as per the FHIRcast standard.
        /// Used for requesting new subscriptions or unsubscribing
        /// </summary>
        /// <returns>StringContent containing the SubscriptionRequest properties to be used in subscription requests</returns>
        public HttpContent BuildPostHttpContent()
        {
            string content = $"hub.callback={Callback}" +
                                $"&hub.mode={Mode}" +
                                $"&hub.topic={Topic}" +
                                $"&hub.secret={Secret}" +
                                $"&hub.events={string.Join(",", Events)}" +
                                $"&hub.lease_seconds={Lease_Seconds}";

            return new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
        } 
        #endregion

        #region Overrides
        /// <summary>
        /// Tests for equality based on the callback, then falls back to the base equality comparison
        /// </summary>
        /// <param name="obj">Object comparing to</param>
        /// <returns>True if the objects are equal</returns>
        public override bool Equals(object obj)
        {
            if ((obj == null) || (!(obj is SubscriptionBase)))
            {
                return false;
            }
            else
            {
                if (obj is SubscriptionRequest)
                {
                    SubscriptionRequest that = (SubscriptionRequest)obj;
                    if (!Callback.Equals(that.Callback))
                    {
                        return false;
                    }
                }
                return base.Equals(obj);
            }
        }

        /// <summary>
        /// Not really used, but added for completeness
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = -1510589517;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Callback);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Secret);
            hashCode = hashCode * -1521134295 + EqualityComparer<SubscriptionChannelType?>.Default.GetHashCode(ChannelType);
            hashCode = hashCode * -1521134295 + EqualityComparer<HubDetails>.Default.GetHashCode(HubDetails);
            return hashCode;
        }
        #endregion
    }

    /// <summary>
    /// Represents a subscription verification that is sent or received in response to a subscription request.
    /// Will either contain the contents of the original subscription request with a challenge that the subscriber
    /// uses to verify the subscription, or it will have Mode set to "denied" with a optional Reason why the
    /// subscription request was denied.
    /// </summary>
    public class SubscriptionVerification : SubscriptionBase
    {
        #region Properties
        public string Challenge { get; set; }

        public string Reason { get; set; }

        public SubscriptionRequest SubscriptionRequest { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Builds the Uri to be sent to the subscriber. Refer to SubscriptionValidator
        /// </summary>
        /// <returns></returns>
        public Uri VerificationURI()
        {
            List<string> queryParams = new List<string>();
            queryParams.Add($"hub.mode={HttpUtility.UrlEncode(this.Mode.ToString())}");
            queryParams.Add($"hub.topic={HttpUtility.UrlEncode(this.Topic)}");
            queryParams.Add($"hub.events={HttpUtility.UrlEncode(String.Join(",", this.Events))}");
            queryParams.Add($"hub.lease_seconds={HttpUtility.UrlEncode(this.Lease_Seconds.ToString())}");

            if (this.Mode == SubscriptionMode.denied)
            {
                queryParams.Add($"hub.reason={HttpUtility.UrlEncode(this.Reason)}");
            }
            else if (this.Mode == SubscriptionMode.subscribe)
            {
                queryParams.Add($"hub.challenge={HttpUtility.UrlEncode(this.Challenge)}");
            }

            var verificationUri = new UriBuilder(SubscriptionRequest.Callback);
            verificationUri.Query += String.Join("&", queryParams.ToArray());

            return verificationUri.Uri;
        }

        /// <summary>
        /// Creates a subscription verification object based on a subscription request.
        /// </summary>
        /// <param name="subscriptionRequest"></param>
        /// <param name="denied"></param>
        /// <returns></returns>
        public static SubscriptionVerification CreateSubscriptionVerification(SubscriptionRequest subscriptionRequest, bool denied = false)
        {
            SubscriptionVerification verification = new SubscriptionVerification()
            {
                SubscriptionRequest = subscriptionRequest,
                Mode = denied ? SubscriptionMode.denied : subscriptionRequest.Mode,
                Topic = subscriptionRequest.Topic,
                Events = subscriptionRequest.Events,
                Lease_Seconds = subscriptionRequest.Lease_Seconds
            };

            if (denied)
            {
                verification.Reason = "Because I said so!";
            }
            else
            {
                verification.Challenge = Guid.NewGuid().ToString("n");
            }

            return verification;
        }
        #endregion
    }
    public class HubDetails
    {
        public string HubUrl { get; set; }
        public string[] HttpHeaders { get; set; }
    }
    public enum SubscriptionMode
    {
        subscribe,
        unsubscribe,
        denied,
    }

    public enum SubscriptionChannelType
    {
        websocket
    }
}
