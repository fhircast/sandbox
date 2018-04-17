using FHIRcastSandbox.Model;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Rules {
    public class Notifications : INotifications {
        private ILogger<Notifications> logger;

        public Notifications(ILogger<Notifications> logger) {
            this.logger = logger;
        }

        public async Task SendNotification(Notification notification, Subscription subscription) {
            this.logger.LogInformation($"Sending notification {notification} to callback {subscription.Callback}");

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(notification);
            var content = new StringContent(str);
            var client = new HttpClient();
            var result = await client.PostAsync(subscription.Callback, content);
        }
    }
}
