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

            SubscriptionBase callbackParameters = null;
            if (outcome == HubValidationOutcome.Canceled) {
                logger.LogDebug("Simulating canceled subscription.");

                callbackParameters = new SubscriptionCancelled
                {
                    Reason = $"The subscription {subscription} was canceled for testing purposes.",
                };
            } else {
                logger.LogDebug("Verifying subscription.");

                callbackParameters = new SubscriptionVerification
                {
                    // Note that this is not necessarily cryptographically random/secure.
                    Challenge = Guid.NewGuid().ToString("n"),
                    LeaseSeconds = subscription.LeaseSeconds
                };

            }

            // Default parametres for both cancel/verify.
            callbackParameters.Callback = subscription.Callback;
            callbackParameters.Events = subscription.Events;
            callbackParameters.Mode = subscription.Mode;
            callbackParameters.Topic = subscription.Topic;

            logger.LogDebug($"Calling callback url: {subscription.Callback}");
            var callbackUri = new SubscriptionCallback().GetCallbackUri(subscription, callbackParameters);
            var response = await new HttpClient().GetAsync(callbackUri);

            if (!response.IsSuccessStatusCode) {
                logger.LogInformation($"Status code was not success but instead {response.StatusCode}");
                return ClientValidationOutcome.NotVerified;
            }
            if (outcome == HubValidationOutcome.Valid) {
                var challenge = ((SubscriptionVerification)callbackParameters).Challenge;
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
