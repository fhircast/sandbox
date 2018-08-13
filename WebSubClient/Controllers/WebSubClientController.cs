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
        [HttpPost("notify")]
        public async Task<IActionResult> Post([FromForm] ClientModel model) {
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(this.Request.Scheme + "://" + this.Request.Host + "/api/hub/notify", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));

            return View(model);
        }
    }
}

