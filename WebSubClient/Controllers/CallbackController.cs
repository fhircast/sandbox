using FHIRcastSandbox.Hubs;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FHIRcastSandbox.WebSubClient.Controllers
{
    [Route("callback")]
    public class CallbackController : Controller
    {
        private readonly IConfiguration _config;
        private readonly WebSubClientHub _clientHub;
        private readonly ILogger<CallbackController> _logger;
        private readonly Subscriptions _subscriptions;

        public CallbackController(IConfiguration config, WebSubClientHub hub, ILogger<CallbackController> logger, Subscriptions subscriptions)
        {
            _config = config;
            _clientHub = hub;
            _logger = logger;
            _subscriptions = subscriptions;
        }

        /// <summary>
        /// Called by hub we sent a subscription request to. They are attempting to verify the subscription.
        /// If the subscription matches one that we have sent out previously that hasn't been verified yet
        /// then return their challenge value, otherwise return a NotFound error response.
        /// </summary>
        /// <param name="clientId">SignalR clientId used in the callback URL</param>
        /// <param name="verification">Hub's verification response to our subscription attempt</param>
        /// <returns>challenge parameter if subscription is verified</returns>
        [HttpGet("{clientId}")]
        public async Task<IActionResult> SubscriptionVerification(string clientId, [Bind(Prefix = "hub")][FromQuery] Common.Model.SubscriptionVerification verification)
        {
            if (!_config.GetValue("Settings:ValidateSubscriptionValidations", true))
            {
                return Content(verification.Challenge);
            }

            _logger.LogDebug($"Recieved subscription verification for {clientId}: {verification.ToString()}");

            if (verification.Mode == Common.Model.SubscriptionMode.denied)
            {
                // SHALL respond with a 200 code
                return Content("");
            }

            if (_subscriptions.VerifiedSubscription(clientId, verification))
            {
                _logger.LogDebug($"Found matching subscription, echoing challenge");
                // have a matching subscription and activated it, 
                // inform client so it can update UI and echo challenge back to app
                await _clientHub.SubscriptionsChanged(clientId);
                return Content(verification.Challenge);
            }
            else
            {
                _logger.LogError($"Did not find matching subscription, not verifying subscription");
                // we don't have a matching pending subscription so don't verify
                return Content("");
            }
        }

        /// <summary>
        /// A client we subscribed to is posting a notification to us
        /// </summary>
        /// <param name="clientId">SignalR clientId used in the callback URL</param>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        [HttpPost("{clientId}")]
        public async Task<IActionResult> Notification(string clientId)
        { //, [FromBody] Notification notification) {
            Notification notification;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                notification = Model.Notification.FromJson(reader.ReadToEnd());
            }

            _logger.LogDebug($"Received notification for {clientId}: {notification.ToString()}");

            // If we have a matching subscription then notify client to update UI and respond with a success code
            if (_subscriptions.HasMatchingSubscription(clientId, notification))
            {
                await _clientHub.ReceivedNotification(clientId, notification);
                return this.Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
