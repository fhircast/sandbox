using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Controllers;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;

namespace FHIRcastSandbox.Controllers {
    [Route("")]
    public class HomeController : Controller {
        public IActionResult Index() {
            return this.RedirectToActionPermanent(
                nameof(WebSubClientController.Get),
                nameof(WebSubClientController).Replace("Controller", ""));
        }
    }

    [Route("client")]
    public class WebSubClientController : Controller {

        private readonly ILogger<WebSubClientController> logger;
        private readonly ClientSubscriptions clientSubscriptions;
        private readonly IHubSubscriptions hubSubscriptions;

        public WebSubClientController(ILogger<WebSubClientController> logger, ClientSubscriptions clientSubscriptions, IHubSubscriptions hubSubscriptions) {
            this.logger = logger;
            this.clientSubscriptions = clientSubscriptions;
            this.hubSubscriptions = hubSubscriptions;
        }

        [HttpGet]
        public IActionResult Get() => View(nameof(WebSubClientController).Replace("Controller", ""), new ClientModel());

        /// <summary>
        /// Called when the client updates its context. Lets the hub know of the changes so it
        /// can notify any subscribing apps.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("notify")]
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] ClientModel model) {
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(this.Request.Scheme + "://" + this.Request.Host + "/api/hub/notify", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));

            return View(model);
        }

        [Route("subscriptions")]
        [HttpPost]
        public async Task<IActionResult> Subscribe(string subscriptionUrl, string topic, string events, string connectionId) {
            subscriptionUrl = subscriptionUrl ?? new UriBuilder(this.Request.Scheme, "localhost", 5000, "/api/hub").Uri.ToString();
            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[64];
            rngCsp.GetBytes(buffer);
            var secret = Convert.ToBase64String(buffer);
            string subUID = Guid.NewGuid().ToString("n");
            var callbackUri = new UriBuilder(
                this.Request.Scheme,
                "localhost",
                this.HttpContext.Connection.LocalPort,
                $"/callback/{subUID}");

            var subscription = new Subscription()
            {
                UID = subUID,
                Callback = callbackUri.Uri,
                Events = events.Split(";", StringSplitOptions.RemoveEmptyEntries),
                Mode = SubscriptionMode.subscribe,
                Secret = secret,
                LeaseSeconds = 3600,
                Topic = topic,
                HubURL = subscriptionUrl,
            };

            // First adding to pending and then sending the subscription to
            // prevent a race.
            this.clientSubscriptions.AddPendingSubscription(connectionId, subscription);
            try {
                await this.hubSubscriptions.SubscribeAsync(subscription);
            }
            catch {
                this.clientSubscriptions.RemoveSubscription(connectionId);
                throw;
            }

            return this.Ok();
        }

        [Route("subscriptions/{subscriptionId}")]
        [HttpDelete]
        public async Task<IActionResult> Unsubscribe(string subscriptionId) {
            this.logger.LogDebug($"Unsubscribing subscription {subscriptionId}");
            Subscription sub = this.clientSubscriptions.GetSubscription(subscriptionId);
            sub.Mode = SubscriptionMode.unsubscribe;

            await this.hubSubscriptions.Unsubscribe(sub);
            this.clientSubscriptions.RemoveSubscription(subscriptionId);

            return this.Ok();
        }
    }
}

