using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using FHIRcastSandbox.Model;

namespace FHIRcastSandbox.Core {
    public class SubscriptionCallback {
        public Uri GetCallbackUri(Subscription subscription, SubscriptionBase parameters) {
            var properties = parameters.GetType().GetProperties()
                .Where(x => x.GetValue(parameters, null) != null)
                .Select(x => x.Name?.ToLower() + "=" + HttpUtility.UrlEncode(x.GetValue(parameters, null).ToString()));

            var addedParamteres = String.Join("&", properties.ToArray());

            var newUri = new UriBuilder(subscription.Callback);
            newUri.Query += addedParamteres;

            return newUri.Uri;
        }
    }
}
