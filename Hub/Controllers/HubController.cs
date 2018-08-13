using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class HubController : Controller {
        private readonly ILogger<HubController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly ISubscriptions subscriptions;
        private readonly INotifications notifications;

        public HubController(ILogger<HubController> logger, IBackgroundJobClient backgroundJobClient, ISubscriptions subscriptions, INotifications notifications) {
            this.backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        }

        /// <summary>
        /// Adds a subscription to this hub.
        /// </summary>
        /// <param name="hub">The subscription parameters.</param>
        /// <param name="_cancel">if set to <c>true</c> simulate cancelling/denying the subscription by sending this to the callback url.</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Subscribe([FromForm]Subscription hub, bool _cancel = false) {
            this.logger.LogDebug($"Model valid state is {this.ModelState.IsValid}");
            foreach (var modelProperty in this.ModelState) {
                if (modelProperty.Value.Errors.Count > 0) {
                    for (int i = 0; i < modelProperty.Value.Errors.Count; i++) {
                        this.logger.LogDebug($"Error found for {modelProperty.Key}: {modelProperty.Value.Errors[i].ErrorMessage}");
                    }
                }
            }

            this.logger.LogDebug($"Subscription for 'received hub subscription': {Environment.NewLine}{hub}");

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
        public async Task<IActionResult> Notify([FromBody] Notification notification) {
            this.logger.LogInformation($"Got notification from client: {notification}");

            var subscriptions = this.subscriptions.GetSubscriptions(notification.Event.Topic, notification.Event.Event);
            this.logger.LogDebug($"Found {subscriptions.Count} subscriptions matching client event");

            //var notification = new Notification
            //{
            //    Timestamp = DateTime.UtcNow,
            //    Id = Guid.NewGuid().ToString("n"),
            //};
            //notification.Event.Topic = notification.Topic;
            //notification.Event.Event = notification.Event;
            //notification.Event.Context = new object[] {
            //    notification.UserIdentifier,
            //    notification.PatientIdentifier,
            //    notification.PatientIdIssuer,
            //    notification.AccessionNumber,
            //    notification.AccessionNumberGroup,
            //    notification.StudyId,
            //};

            foreach (var sub in subscriptions) {
                await this.notifications.SendNotification(notification, sub);
            }

            return this.Ok();
        }
    }
}
