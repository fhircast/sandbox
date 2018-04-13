using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using FHIRcastSandbox.Model;

namespace FHIRcastSandbox.Core {
    public class CallbackClient {
        public Task SendCallback(Subscription subscription, SubscriptionBase parameters) {
            var properties = parameters.GetType().GetProperties()
                .Where(x => x.GetValue(parameters, null) != null)
                .Select(x => x.Name + "=" + HttpUtility.UrlEncode(x.GetValue(parameters, null).ToString()));

            var addedParamteres = String.Join("&", properties.ToArray());

            var newUri = new UriBuilder(subscription.Callback);
            newUri.Query += addedParamteres;

            var client = new HttpClient();
            return client.GetAsync(newUri.Uri);
        }
    }
}
