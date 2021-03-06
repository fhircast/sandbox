using Common.Model;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Controllers
{
    [Route("api/[controller]")]
    public class HubController : Controller
    {
        private readonly ILogger<HubController> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly ISubscriptions subscriptions;
        private readonly INotifications<HttpResponseMessage> notifications;
        private readonly IContexts contexts;

        public HubController(ILogger<HubController> logger, IBackgroundJobClient backgroundJobClient, ISubscriptions subscriptions, INotifications<HttpResponseMessage> notifications, IContexts contexts)
        {
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
        public IActionResult Subscribe([FromForm]SubscriptionRequest hub, bool _cancel = false)
        {
            logger.LogDebug($"Model valid state is {this.ModelState.IsValid}");
            foreach (var modelProperty in this.ModelState)
            {
                if (modelProperty.Value.Errors.Count > 0)
                {
                    for (int i = 0; i < modelProperty.Value.Errors.Count; i++)
                    {
                        logger.LogDebug($"Error found for {modelProperty.Key}: {modelProperty.Value.Errors[i].ErrorMessage}");
                    }
                }
            }

            logger.LogDebug($"Subscription for 'received hub subscription': {Environment.NewLine}{hub}");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            backgroundJobClient.Enqueue<ValidateSubscriptionJob>(job => job.Run(hub, _cancel));

            return Accepted();
        }

        /// <summary>
        /// Gets all active subscriptions.
        /// </summary>
        /// <returns>All active subscriptions.</returns>
        [HttpGet]
        public IEnumerable<SubscriptionRequest> GetSubscriptions()
        {
            return subscriptions.GetActiveSubscriptions();
        }

        /// <summary>
        /// Sets a context for a certain topic.
        /// </summary>
        /// <returns></returns>
        [Route("{topicId}")]
        [HttpPost]
        public async Task<IActionResult> Notify(string topicId)
        {
            Notification notification;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                notification = Notification.FromJson(await reader.ReadToEndAsync());
            }

            logger.LogInformation($"Got notification from client: {notification}");

            var subscriptions = this.subscriptions.GetSubscriptions(notification.Event.Topic, notification.Event.Event);
            logger.LogDebug($"Found {subscriptions.Count} subscriptions matching client event");

            if (subscriptions.Count == 0)
            {
                return NotFound($"Could not find any subscriptions for sessionId {topicId}.");
            }

            contexts.setContext(topicId, notification.Event.Context);

            var success = true;
            foreach (var sub in subscriptions)
            {
                success |= (await notifications.SendNotification(notification, sub)).IsSuccessStatusCode;
            }
            if (!success)
            {
                // TODO: return reason for failure
                Forbid();
            }
            return Ok();
        }

        /// <summary>
        /// TODO: Looks like the query for current context functionality. Not sure where this will be after 
        /// ballot resolution so look into this later.
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        [Route("{topicId}")]
        [HttpGet]
        public object GetCurrentcontext(string topicId)
        {
            logger.LogInformation($"Got context request from for : {topicId}");

            var context = contexts.getContext(topicId);

            if (context != null)
            {
                return context;
            }
            else
            {
                return NotFound();
            }

        }
    }
}
