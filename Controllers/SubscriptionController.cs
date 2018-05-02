using FHIRcastSandbox.Model;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Controllers
{
    [Route("api/[controller]")]
    public class SubscriptionController : Controller
    {
        private readonly ILogger<HubController> logger;
        private readonly ISubscriptions subscriptions;

        public SubscriptionController(ILogger<HubController> logger, IBackgroundJobClient backgroundJobClient, ISubscriptions subscriptions, INotifications notifications)
        {
            this.logger = logger;
            this.subscriptions = subscriptions;
        }

        [HttpGet("{subscriptionId}")]
        public IActionResult Get([FromQuery] SubscriptionVerification verification)
        {
            return this.Content(verification.Challenge);
        }

        [HttpPost("{subscriptionId}")]
        public IActionResult Post(string subscriptionId, [FromBody] Notification notification)
        {
            return this.Ok(notification);
        }

    }
}
