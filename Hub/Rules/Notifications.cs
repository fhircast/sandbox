using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using FHIRcastSandbox.Model.Http;
using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Rules {
    public class Notifications : INotifications {
        private ILogger<Notifications> logger;

        public Notifications(ILogger<Notifications> logger) {
            this.logger = logger;
        }

        public async Task SendNotification(Notification notification, Subscription subscription) {
            this.logger.LogInformation($"Sending notification {notification} to callback {subscription.Callback}");

            var client = new HttpClient();
            var content = notification.CreateHttpContent(subscription.Secret);
            var response = await client.PostAsync(subscription.Callback, content);

            this.logger.LogDebug($"Got response from posting notification:{Environment.NewLine}{response}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}.");
        }
    }
}
