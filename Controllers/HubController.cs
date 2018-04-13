using System;
using System.Net.Http;
using System.Threading.Tasks;
using FHIRcastSandbox.Core;
using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class HubController : Controller {
        private readonly ILogger<HubController> logger;

        public HubController(ILogger<HubController> logger) {
            this.logger = logger;
        }

        /// <summary>
        /// Adds a subscription to this hub.
        /// </summary>
        /// <param name="hub">The subscription parameters</param>
        /// <param name="_cancelSubscription">if set to <c>true</c> simulate cancelling/denying the subscription by sending this to the callback url.</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromForm]Subscription hub, bool _cancelSubscription) {
            this.logger.LogDebug($"Model valid state is {this.ModelState.IsValid}");
            this.logger.LogDebug($"Received hub subscription: {JsonConvert.SerializeObject(hub)}");

            if (!this.ModelState.IsValid) {
                return this.BadRequest(this.ModelState);
            }

            return this.Accepted();
        }
    }
}
