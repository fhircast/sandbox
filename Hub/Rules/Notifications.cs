using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace FHIRcastSandbox.Rules {
    public class Notifications<T> : INotifications<HttpResponseMessage> {
        private ILogger<Notifications<HttpResponseMessage>> logger;

        public Notifications(ILogger<Notifications<HttpResponseMessage>> logger) {
            this.logger = logger;
        }

        public async Task<HttpResponseMessage> SendNotification(Notification notification, Subscription subscription) {
            this.logger.LogInformation($"Sending notification {notification} to callback {subscription.Callback}");

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(notification);
            var content = new StringContent(str);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var client = new HttpClient();
            var response = await client.PostAsync(subscription.Callback, content);

            this.logger.LogDebug($"Got response from posting notification:{Environment.NewLine}{response}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}.");

            return response;
        }
    }
}
