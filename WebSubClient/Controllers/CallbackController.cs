using System.Threading.Tasks;
using FHIRcastSandbox.Hubs;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.WebSubClient.Controllers
{
    [Route("callback")]
    public class CallbackController : Controller {
        private readonly ClientSubscriptions clientSubscriptions;
        private readonly IHubContext<WebSubClientHub, IWebSubClient> webSubClientHubContext;
        private readonly IConfiguration config;
        private readonly WebSubClientHub clientHub;
        private readonly ILogger<CallbackController> logger;

        public CallbackController(ClientSubscriptions clientSubscriptions, IHubContext<WebSubClientHub, IWebSubClient> webSubClientHubContext, IConfiguration config, WebSubClientHub hub, ILogger<CallbackController> logger) {
            this.clientSubscriptions = clientSubscriptions;
            this.webSubClientHubContext = webSubClientHubContext;
            this.config = config;
            this.clientHub = hub;
            this.logger = logger;
        }

        /// <summary>
        /// Called by hub we sent a subscription request to. They are attempting to verify the subscription.
        /// If the subscription matches one that we have sent out previously that hasn't been verified yet
        /// then return their challenge value, otherwise return a NotFound error response.
        /// </summary>
        /// <param name="connectionId">SignalR connectionId used in the callback URL</param>
        /// <param name="verification">Hub's verification response to our subscription attempt</param>
        /// <returns>challenge parameter if subscription is verified</returns>
        [HttpGet("{connectionId}")]
        public IActionResult SubscriptionVerification(string connectionId, [FromQuery] SubscriptionVerification verification) {
            if (!this.config.GetValue("Settings:ValidateSubscriptionValidations", true)) {
                return this.Content(verification.Challenge);
            }
            
            var verificationValidation = this.clientSubscriptions.ValidateVerification(connectionId, verification);

            if (verification.Mode == SubscriptionMode.denied)
            {
                this.clientSubscriptions.RemoveSubscription(connectionId, verification.Topic);
                this.clientHub.AlertMessage(connectionId, $"Error subscribing to {verification.Topic}: {verification.Reason}");
                return this.Content("");
            }
            else
            {
                switch (verificationValidation)
                {
                    case SubscriptionVerificationValidation.IsPendingVerification:
                        this.clientSubscriptions.ActivateSubscription(verification.Topic);
                        break;
                    case SubscriptionVerificationValidation.DoesNotExist:
                        return this.NotFound();
                    case SubscriptionVerificationValidation.IsAlreadyActive:
                        break;
                    case SubscriptionVerificationValidation.IsPendingDeletion:
                        this.clientSubscriptions.RemoveSubscription(connectionId, verification.Topic);
                        break;
                    default:
                        break;
                }

                this.clientHub.AddSubscription(connectionId, new SubscriptionWithHubURL(this.clientSubscriptions.GetSubscription(connectionId, verification.Topic)));

                return this.Content(verification.Challenge);
            }           
        }

        /// <summary>
        /// A client we subscribed to is posting a notification to us
        /// </summary>
        /// <param name="topicId">The subscription identifier.</param>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        [HttpPost("{topicId}")]
        public async Task<IActionResult> Notification(string topicId, [FromBody] Notification notification) {
            //If we do not have an active subscription matching the id then return a notfound error
            var clients = this.clientSubscriptions.GetSubscribedClients(notification);

            await this.clientHub.ReceivedNotification(clients[0], notification);

            return this.Ok();
        }
    }
}
