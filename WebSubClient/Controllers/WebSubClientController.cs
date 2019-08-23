using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return this.RedirectToActionPermanent(
                nameof(WebSubClientController.Get),
                nameof(WebSubClientController).Replace("Controller", ""));
        }
    }

    [Route("client")]
    public class WebSubClientController : Controller
    {

        private readonly ILogger<WebSubClientController> logger;

        public WebSubClientController(ILogger<WebSubClientController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Get() => this.View(nameof(WebSubClientController).Replace("Controller", ""), new ClientModel());
    }
}

