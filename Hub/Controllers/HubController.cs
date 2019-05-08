using System;
using System.Collections.Generic;
using System.Net.Http;
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
        private readonly INotifications<HttpResponseMessage> notifications;
        private readonly IContexts contexts;

        public HubController(ILogger<HubController> logger, IBackgroundJobClient backgroundJobClient, ISubscriptions subscriptions, INotifications<HttpResponseMessage> notifications, IContexts contexts) {
            this.backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        }

        /// <summary>
        /// Adds a subscription to this hub.
        /// </summary>
        /// <param name="hub">The subscription parameters.</param>
        /// <param name="_cancel">if set to <c>true</c> simulate cancelling/denying the subscription by sending this to the callback url.</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Subscribe([FromForm]Subscription hub, bool _cancel = false)
        {
            this.logger.LogDebug($"Subscription for 'received hub subscription': {Environment.NewLine}{hub}");
            if (!hub.ValidModelState(SubscriptionModelStates.Request))
            {
                //TODO: Add error logging and an error message to the bad request
                return this.BadRequest();
            }

            this.backgroundJobClient.Enqueue<ValidateSubscriptionJob>(job => job.Run(hub, _cancel));

            return this.Accepted();
        }

        /// <summary>
        /// Gets all active subscriptions.
        /// </summary>
        /// <returns>All active subscriptions.</returns>
        [HttpGet]
        public IEnumerable<Subscription> GetSubscriptions()
        {
            return this.subscriptions.GetActiveSubscriptions();
        }

        /// <summary>
        /// Sets a context for a certain topic.
        /// </summary>
        /// <returns></returns>
        [Route("{topicId}")]
        [HttpPost]
        public async Task<IActionResult> Notify(string topicId, [FromBody] Notification notification)
        {
            this.logger.LogInformation($"Got notification from client: {notification}");

            var subscriptions = this.subscriptions.GetSubscriptions(notification.Event.Topic, notification.Event.Event);
            this.logger.LogDebug($"Found {subscriptions.Count} subscriptions matching client event");

            if (subscriptions.Count == 0)
            {
                return this.NotFound($"Could not find any subscriptions for sessionId {topicId}.");
            }

            contexts.setContext(topicId, notification.Event.Context);

            var success = true;
            foreach (var sub in subscriptions)
            {
                success |= (await this.notifications.SendNotification(notification, sub)).IsSuccessStatusCode;
            }
            if (!success)
            {
                // TODO: return reason for failure
                this.Forbid();
            }
            return this.Ok();
        }

        [Route("{topicId}")]
        [HttpGet]
        public object GetCurrentcontext(string topicId) {
            this.logger.LogInformation($"Got context request from for : {topicId}");

            var context = contexts.getContext(topicId);

            if (context != null){
                return context;
            }
            else
            {
                return this.NotFound();
            }

        }
    }
}
