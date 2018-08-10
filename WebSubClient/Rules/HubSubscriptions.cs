using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.WebSubClient.Rules {
    public class HubSubscriptions : IHubSubscriptions {
        private readonly ILogger<HubSubscriptions> logger;
        public HubSubscriptions(ILogger<HubSubscriptions> logger) {
            this.logger = logger;
        }

        public async Task SubscribeAsync(Subscription subscription) {

            string content = $"hub.callback={subscription.Callback}" +
                                $"&hub.mode={subscription.Mode}" +
                                $"&hub.topic={subscription.Topic}" +
                                $"&hub.secret={subscription.Secret}" +
                                $"&hub.events={string.Join(",", subscription.Events)}" +
                                $"&hub.lease_seconds={subscription.LeaseSeconds}" +
                                $"&hub.UID={subscription.UID}";

            StringContent httpcontent = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

            this.logger.LogDebug($"Posting async to {subscription.HubURL}: {content}");

            var result = await new HttpClient().PostAsync(subscription.HubURL, httpcontent);
        }

        public async Task Unsubscribe(Subscription subscription) {
            var httpClient = new HttpClient();
            var result = await httpClient.PostAsync(subscription.HubURL,
                new StringContent(
                    $"hub.callback={subscription.Callback}" +
                    $"&hub.mode={subscription.Mode}" +
                    $"&hub.topic={subscription.Topic}" +
                    $"&hub.secret={subscription.Secret}" +
                    $"&hub.events={string.Join(",", subscription.Events)}" +
                    $"&hub.lease_seconds={subscription.LeaseSeconds}" +
                    $"&hub.UID={subscription.UID}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"));

            result.EnsureSuccessStatusCode();
        }
    }
}
