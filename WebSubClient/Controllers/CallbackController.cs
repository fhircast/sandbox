using System.Threading.Tasks;
using FHIRcastSandbox.Hubs;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace FHIRcastSandbox.WebSubClient.Controllers {
    [Route("callback")]
    public class CallbackController : Controller {
        private readonly ClientSubscriptions clientSubscriptions;
        private readonly IHubContext<WebSubClientHub> webSubClientHubContext;
        private readonly IConfiguration config;

        public CallbackController(ClientSubscriptions clientSubscriptions, IHubContext<WebSubClientHub> webSubClientHubContext, IConfiguration config) {
            this.clientSubscriptions = clientSubscriptions;
            this.webSubClientHubContext = webSubClientHubContext;
            this.config = config;
        }

        /// <summary>
        /// Called by hub we sent a subscription request to. They are attempting to verify the subscription.
        /// If the subscription matches one that we have sent out previously that hasn't been verified yet
        /// then return their challenge value, otherwise return a NotFound error response.
        /// </summary>
        /// <param name="subscriptionId">ID of the subscription, part of the url</param>
        /// <param name="verification">Hub's verification response to our subscription attempt</param>
        /// <returns></returns>
        [HttpGet("{subscriptionId}")]
        public IActionResult SubscriptionVerification(string subscriptionId, [FromQuery] SubscriptionVerification hub) {
            var verificationValidation = this.clientSubscriptions.ValidateVerification(hub);

            if (this.config.GetValue("Settings:ValidateSubscriptionValidations", true)) {
                switch (verificationValidation) {
                    case SubscriptionVerificationValidation.IsPendingVerification:
                        this.clientSubscriptions.ActivateSubscription(hub.UID);
                        break;
                    case SubscriptionVerificationValidation.DoesNotExist:
                        return this.NotFound();
                    case SubscriptionVerificationValidation.IsAlreadyActive:
                        break;
                    default:
                        break;
                }
            }

            return this.Content(hub.Challenge);
        }

        /// <summary>
        /// Posts the specified subscription identifier.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        [HttpPost("{subscriptionId}")]
        public async Task<IActionResult> Notification(string subscriptionId, [FromBody] Notification notification) {
            //If we do not have an active subscription matching the id then return a notfound error
            var clients = this.clientSubscriptions.GetSubscribedClients(notification);

            //var internalModel = new ClientModel()
            //{
            //    UserIdentifier = notification.Event.Context[0] == null ? "" : notification.Event.Context[0].ToString(),
            //    PatientIdentifier = notification.Event.Context[1] == null ? "" : notification.Event.Context[1].ToString(),
            //    PatientIdIssuer = notification.Event.Context[2] == null ? "" : notification.Event.Context[2].ToString(),
            //    AccessionNumber = notification.Event.Context[3] == null ? "" : notification.Event.Context[3].ToString(),
            //    AccessionNumberGroup = notification.Event.Context[4] == null ? "" : notification.Event.Context[4].ToString(),
            //    StudyId = notification.Event.Context[5] == null ? "" : notification.Event.Context[5].ToString(),
            //};

            await this.webSubClientHubContext.Clients.Clients(clients)
                .SendAsync("notification", notification);

            return this.Ok();
        }
    }
}
