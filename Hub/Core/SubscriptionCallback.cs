using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System;

namespace FHIRcastSandbox.Core {
    public class SubscriptionCallback {
        public Uri GetCallbackUri(Subscription subscription, SubscriptionBase parameters) {
            var properties = parameters.GetType().GetProperties()
                .Where(x => x.GetValue(parameters, null) != null)
                .Select(x => this.GetFieldName(x) + "=" + HttpUtility.UrlEncode(x.GetValue(parameters, null).ToString()));

            var addedParamteres = String.Join("&", properties.ToArray());

            var newUri = new UriBuilder(subscription.Callback);
            newUri.Query += addedParamteres;

            return newUri.Uri;
        }

        private string GetFieldName(PropertyInfo property) {
            var attr = property.GetCustomAttribute<URLNameOverride>();
            if (attr == null) { return property.Name.ToLower(); }
            else { return attr.Value; }
        }
    }
}
