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
    }
}

