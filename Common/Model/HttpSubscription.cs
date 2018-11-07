using System.Net.Http;
using FHIRcastSandbox.Model;

namespace FHIRcastSandbox.Model.Http {
    public static class SubscriptionExtensions {
        public static HttpContent CreateHttpContent(this Subscription source) {

            string content = $"hub.callback={source.Callback}" +
                $"&hub.mode={source.Mode}" +
                $"&hub.topic={source.Topic}" +
                $"&hub.secret={source.Secret}" +
                $"&hub.events={string.Join(",", source.Events)}" +
                $"&hub.lease_seconds={source.LeaseSeconds}" +
                $"&hub.UID={source.UID}";

            StringContent httpcontent = new StringContent(
                    content,
                    System.Text.Encoding.UTF8,
                    "application/x-www-form-urlencoded");

            return httpcontent;
        }
    }
}

