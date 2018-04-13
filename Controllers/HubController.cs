using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class HubController : Controller {
        private readonly ILogger<HubController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;

        public HubController(ILogger<HubController> logger, IBackgroundJobClient backgroundJobClient) {
            this.backgroundJobClient = backgroundJobClient;
            this.logger = logger;
        }

        /// <summary>
        /// Adds a subscription to this hub.
        /// </summary>
        /// <param name="hub">The subscription parameters</param>
        /// <param name="_cancel">if set to <c>true</c> simulate cancelling/denying the subscription by sending this to the callback url.</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromForm]Subscription hub, bool _cancel = false) {
            this.logger.LogDebug($"Model valid state is {this.ModelState.IsValid}");
            this.logger.LogDebug($"Received hub subscription: {JsonConvert.SerializeObject(hub)}");

            if (!this.ModelState.IsValid) {
                return this.BadRequest(this.ModelState);
            }

            this.backgroundJobClient.Enqueue<ISubscriptionValidator>(validator => validator.ValidateSubscription(hub, _cancel ? HubValidationOutcome.Canceled : HubValidationOutcome.Valid));

            return this.Accepted();
        }
    }
}
