using System.Net.Http.Headers;
using System.Net.Http;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;

namespace FHIRcastSandbox.Model.Http {
    public static class SubscriptionExtensions {
        public static HttpContent CreateHttpContent(this Subscription source) {

            string content = $"hub.callback={source.Callback}" +
                $"&hub.mode={source.Mode}" +
                $"&hub.topic={source.Topic}" +
                $"&hub.secret={source.Secret}" +
                $"&hub.events={string.Join(",", source.Events)}" +
                $"&hub.lease_seconds={source.LeaseSeconds}";

            StringContent httpcontent = new StringContent(
                    content,
                    System.Text.Encoding.UTF8,
                    "application/x-www-form-urlencoded");

            return httpcontent;
        }
    }

    public static class NotificationExtensions {
        public static HttpContent CreateHttpContent(this Notification source, string subscriptionSecret = null) {
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(source);
            var content = new StringContent(str);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            if (subscriptionSecret != null) {
                var hubSignature = new HmacDigest().CreateHubSignature(subscriptionSecret, str);
                content.Headers.Add("X-Hub-Signature", hubSignature);
            }

            return content;
        }
    }
}
