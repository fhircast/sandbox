using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.WebSubClient.Rules
{
    /// <summary>
    /// This class handles subscribing and unsubscribing to other applications 
    /// from this running client.
    /// </summary>
    public class HubSubscriptions : IHubSubscriptions
    {
        private readonly ILogger<HubSubscriptions> logger;
        public HubSubscriptions(ILogger<HubSubscriptions> logger)
        {
            this.logger = logger;
        }

        public async Task SubscribeAsync(Subscription subscription)
        {

            string content = $"hub.callback={subscription.Callback}" +
                                $"&hub.mode={subscription.Mode}" +
                                $"&hub.topic={subscription.Topic}" +
                                $"&hub.secret={subscription.Secret}" +
                                $"&hub.events={string.Join(",", subscription.Events)}" +
                                $"&hub.lease_seconds={subscription.Lease_Seconds}";

            StringContent httpcontent = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

            this.logger.LogDebug($"Posting async to {subscription.HubURL}: {content}");

            HttpClient client = new HttpClient();
            foreach (string header in subscription.HubURL.HTTPHeaders)
            {
                string[] split = header.Split(":");
                client.DefaultRequestHeaders.Add(split[0], split[1]);
            }
            var result = await client.PostAsync(subscription.HubURL.URL, httpcontent);
        }

        public async Task Unsubscribe(Subscription subscription)
        {

            string content = $"hub.callback={subscription.Callback}" +
                    $"&hub.mode={subscription.Mode}" +
                    $"&hub.topic={subscription.Topic}" +
                    $"&hub.secret={subscription.Secret}" +
                    $"&hub.events={string.Join(",", subscription.Events)}" +
                    $"&hub.lease_seconds={subscription.Lease_Seconds}";

            StringContent httpContent = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

            this.logger.LogDebug($"Posting async to {subscription.HubURL}: {content}");

            var client = new HttpClient();
            foreach (string header in subscription.HubURL.HTTPHeaders)
            {
                string[] split = header.Split(":");
                client.DefaultRequestHeaders.Add(split[0], split[1]);
            }
            var result = await client.PostAsync(subscription.HubURL.URL, httpContent);

            result.EnsureSuccessStatusCode();
        }
    }
}
