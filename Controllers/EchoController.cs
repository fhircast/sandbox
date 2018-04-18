using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using System;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class EchoController : Controller {
        [HttpGet]
        public IActionResult Get([FromQuery] SubscriptionVerification verification) {
            return this.Content(verification.Challenge);
        }

        [HttpPost]
        public IActionResult Post([FromBody] Notification notification) {
            return this.Ok(notification);
        }
    }
}
