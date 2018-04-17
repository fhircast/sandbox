using Microsoft.AspNetCore.Mvc;
using FHIRcastSandbox.Model;

namespace FHIRcastSandbox.Controllers {
    [Route("")]
    public class FHIRcastClientController : Controller {

        private static ClientModel internalModel;

        [HttpGet]
        public IActionResult Get() => View("FHIRcastClient", new ClientModel());

        [HttpPost]
        public IActionResult Post([FromForm] ClientModel model) {

            internalModel = model;
            RedirectToAction("Hub", "Notify", model);

            return View("FHIRcastClient", model);
        }

        [Route("subscribe")]
        [HttpPost]
        public IActionResult Subscribe(string subscriptionUrl) {

            return View("FHIRcastClient", internalModel);
        }
    }
}
