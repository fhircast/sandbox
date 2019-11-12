using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        [BindingBehavior(BindingBehavior.Optional)]
        public string Callback { get; set; }

        [BindingBehavior(BindingBehavior.Optional)]
        public string Secret { get; set; }

        [BindRequired]
        public Channel Channel { get; set; }

        [BindNever]
        public HubDetails HubDetails { get; set; }

        public string WebsocketURL { get; set; }
        public WebSocket Websocket { get; set; }
        /// <summary>
        /// This is the key used to store the subscription in a dictionary (see Hub implementation).
        /// </summary>
        public string CollectionKey
        {
            get
            {
                try
                {
                    if (Channel.Type == SubscriptionChannelType.webhook)
                    {
                        return Callback;
                    }
                    else
                    {
                        return WebsocketURL;
                    }
                }
                catch (Exception)
                {
                    return "";
                }
                
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// This builds the HTTP content used in subscription requests as per the FHIRcast standard.
        /// Used for requesting new subscriptions or unsubscribing
        /// </summary>
        /// <returns>StringContent containing the SubscriptionRequest properties to be used in subscription requests</returns>
        public HttpContent BuildPostHttpContent()
        {
            StringBuilder sb = new StringBuilder();

            if (Channel.Type == SubscriptionChannelType.websocket)
            {
                sb.Append($"hub.channel.type={Channel.Type}");
            }
            else
            {
                sb.Append($"hub.callback={Callback}");
            }

            sb.Append($"&hub.mode={Mode}");
            sb.Append($"&hub.topic={Topic}");
            sb.Append($"&hub.secret={Secret}");
            sb.Append($"&hub.events={string.Join(",", Events)}");
            sb.Append($"&hub.lease_seconds={Lease_Seconds}");

            return new StringContent(sb.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        } 

        public string GetWebsocketUrl(string hubHost, int? port)
        {
            if (Channel.Type != SubscriptionChannelType.websocket)
            {
                throw new Exception("Channel type isn't websocket");
            }

            string guid = Guid.NewGuid().ToString("n");

            Uri uri = new UriBuilder("ws", hubHost, (port == null) ? 0 : port.Value, guid).Uri;
            WebsocketURL = (port == null) ? uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped) : uri.AbsoluteUri;
            return WebsocketURL;
        }

        public async Task<bool> SendNotificationAsync(string jsonBody)
        {           
            if (Channel.Type == SubscriptionChannelType.webhook)
            {
                return await SendWebhookNotificationAsync(jsonBody);
            }
            else if (Channel.Type == SubscriptionChannelType.websocket)
            {
                return await SendWebsocketNotificationAsync(jsonBody);
            }
            return false;
        }
        #endregion

        #region Private Methods
        private async Task<bool> SendWebsocketNotificationAsync(string jsonBody)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(jsonBody);
                var segment = new ArraySegment<byte>(buffer);
                await Websocket.SendAsync(segment, WebSocketMessageType.Text, true, default(CancellationToken));
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> SendWebhookNotificationAsync(string jsonBody)
        {
            HttpContent httpContent = new StringContent(jsonBody);

            // Add the headers
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpContent.Headers.Add("X-Hub-Signature", XHubSignature(jsonBody));

            HttpClient client = new HttpClient();
            var response = await client.PostAsync(this.Callback, httpContent);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Calculates and returns the X-Hub-Signature header. Currently uses sha256
        /// </summary>
        /// <param name="subscription">Subscription to get the secret from</param>
        /// <param name="body">Body used to calculate the signature</param>
        /// <returns>The sha256 hash of the body using the subscription's secret</returns>
        private string XHubSignature(string body)
        {
            using (HMACSHA256 sha256 = new HMACSHA256(Encoding.ASCII.GetBytes(this.Secret)))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

                byte[] hash = sha256.ComputeHash(bodyBytes);
                StringBuilder stringBuilder = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    stringBuilder.AppendFormat("{0:x2}", b);
                }

                return "sha256=" + stringBuilder.ToString();
            }
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
            hashCode = hashCode * -1521134295 + EqualityComparer<SubscriptionChannelType?>.Default.GetHashCode(Channel.Type);
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

    public class Channel
    {
        public SubscriptionChannelType Type { get; set; }
        public string Endpoint { get; set; }    //This isn't used yet
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
        webhook,
        websocket
    }
}
