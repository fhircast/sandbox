using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        public IActionResult Get() => this.View(nameof(WebSubClientController).Replace("Controller", ""), new ClientModel());

        /// <summary>
        /// Called when the client updates its context. Lets the hub know of the changes so it
        /// can notify any subscribing apps.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("notify")]
        public async Task<IActionResult> Post([FromForm] ClientModel model) {
            var httpClient = new HttpClient();
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
            };

            notification.Event.Topic = model.Topic;
            notification.Event.Event = model.Event;

            var interestedClients = this.clientSubscriptions.GetSubscribedClients(notification);

            foreach (var subscriptionId in interestedClients)
            {
                var hubURL = this.clientSubscriptions.GetSubscription(subscriptionId, model.Topic).HubURL;
                var response = await httpClient.PostAsync($"{hubURL.URL}/{model.Topic}", new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }

            return this.View("WebSubClient", model);
        }
    }
}

