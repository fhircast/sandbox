using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class HubController : Controller {
        private readonly ILogger<HubController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly ISubscriptions subscriptions;
        private readonly INotifications notifications;

        public HubController(ILogger<HubController> logger, IBackgroundJobClient backgroundJobClient, ISubscriptions subscriptions, INotifications notifications) {
            this.backgroundJobClient = backgroundJobClient;
            this.logger = logger;
            this.subscriptions = subscriptions;
            this.notifications = notifications;
        }

        /// <summary>
        /// Adds a subscription to this hub.
        /// </summary>
        /// <param name="hub">The subscription parameters</param>
        /// <param name="_cancel">if set to <c>true</c> simulate cancelling/denying the subscription by sending this to the callback url.</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Subscribe([FromForm]Subscription hub, bool _cancel = false) {
            this.logger.LogDebug($"Model valid state is {this.ModelState.IsValid}");
            foreach (var modelProperty in this.ModelState)
            {
                if (modelProperty.Value.Errors.Count > 0)
                {
                    for (int i = 0; i < modelProperty.Value.Errors.Count; i++)
                    {
                        this.logger.LogDebug($"Error found for {modelProperty.Key}: {modelProperty.Value.Errors[i].ErrorMessage}");
                    }
                }
            }
            this.logger.LogInformation($"Received hub subscription: {hub}");

            if (!this.ModelState.IsValid) {
                return this.BadRequest(this.ModelState);
            }

            this.backgroundJobClient.Enqueue<ValidateSubscriptionJob>(job => job.Run(hub, _cancel));

            return this.Accepted();
        }

        /// <summary>
        /// Gets all active subscriptions.
        /// </summary>
        /// <returns>All active subscriptions.</returns>
        [HttpGet]
        public IEnumerable<Subscription> GetSubscriptions() {
            return this.subscriptions.GetActiveSubscriptions();
        }

        [Route("notify")]
        [HttpPost]
        public async Task<IActionResult> Notify([FromBody] ClientModel clientEvent) {
            this.logger.LogInformation($"Got notification from client: {clientEvent}");

            var subscriptions = this.subscriptions.GetSubscriptions(clientEvent.Topic, clientEvent.Event);
            this.logger.LogDebug($"Found {subscriptions.Count} subscriptions matching client event");

            var notification = new Notification {
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("n"),
            };
            notification.Event.Topic = clientEvent.Topic;
            notification.Event.Event = clientEvent.Event;
            notification.Event.Context = new object[] {
                clientEvent.UserIdentifier,
                clientEvent.PatientIdentifier,
                clientEvent.PatientIdIssuer,
                clientEvent.AccessionNumber,
                clientEvent.AccessionNumberGroup,
                clientEvent.StudyId,
            };

            foreach (var sub in subscriptions) {
                await this.notifications.SendNotification(notification, sub);
            }

            return this.Ok();
        }
    }
}
