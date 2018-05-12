using FHIRcastSandbox.Core;
using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace FHIRcastSandbox.Rules {
    public class SubscriptionValidator : ISubscriptionValidator {
        private readonly ILogger logger = null;

        public SubscriptionValidator(ILogger<SubscriptionValidator> logger) {
            this.logger = logger;
        }

        public async Task<ClientValidationOutcome> ValidateSubscription(Subscription subscription, HubValidationOutcome outcome) {
            if (subscription == null) {
                throw new ArgumentNullException(nameof(subscription));
            }


            HttpResponseMessage response = null;
            string challenge = Guid.NewGuid().ToString("n");

            if (outcome == HubValidationOutcome.Canceled) {
                logger.LogDebug("Simulating canceled subscription.");

                SubscriptionBase callbackParameters = new SubscriptionCancelled
                {
                    Reason = $"The subscription {subscription} was canceled for testing purposes.",
                };
            } else {
                logger.LogDebug("Verifying subscription.");

                SubscriptionVerification callbackParameters = new SubscriptionVerification
                {
                    // Note that this is not necessarily cryptographically random/secure.
                    Challenge = challenge,
                    LeaseSeconds = subscription.LeaseSeconds,
                    Callback = subscription.Callback,
                    Events = subscription.Events,
                    Mode = subscription.Mode,
                    Topic = subscription.Topic,
                 };

                logger.LogDebug($"Calling callback url: {subscription.Callback}");
                string verifyUrl = subscription.Callback + "?" +                   
                     $"&hub.mode={callbackParameters.Mode.ToString().ToLower()}" +
                     $"&hub.topic={callbackParameters.Topic}" +
                     $"&hub.challenge={callbackParameters.Challenge}" +
                     $"&hub.events={string.Join(",", callbackParameters.Events)}" +
                     $"&hub.lease_seconds={callbackParameters.LeaseSeconds}";
                response = await new HttpClient().GetAsync(verifyUrl);
            }

       

            if (!response.IsSuccessStatusCode) {
                logger.LogInformation($"Status code was not success but instead {response.StatusCode}");
                return ClientValidationOutcome.NotVerified;
            }
            if (outcome == HubValidationOutcome.Valid) {

                var responseBody = (await response.Content.ReadAsStringAsync());
                if (responseBody != challenge) {
                    logger.LogInformation($"Callback result for verification request was not equal to challenge. Response body: '{responseBody}', Challenge: '{challenge}'.");
                    return ClientValidationOutcome.NotVerified;
                }

                return ClientValidationOutcome.Verified;
            }

            return ClientValidationOutcome.NotVerified;
        }
    }

    public enum HubValidationOutcome {
        Valid,
        Canceled,
    }

    public enum ClientValidationOutcome {
        Verified,
        NotVerified,
    }
}
