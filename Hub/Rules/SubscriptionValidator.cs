using Common.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Rules
{
    public class SubscriptionValidator : ISubscriptionValidator
    {
        private readonly ILogger logger = null;

        public SubscriptionValidator(ILogger<SubscriptionValidator> logger)
        {
            this.logger = logger;
        }

        public async Task<ClientValidationOutcome> ValidateSubscription(SubscriptionRequest subscription, HubValidationOutcome outcome)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            SubscriptionVerification verification = SubscriptionVerification.CreateSubscriptionVerification(subscription, (outcome == HubValidationOutcome.Canceled));
            Uri verificationUri = verification.VerificationURI();

            logger.LogDebug($"Calling callback url: {verificationUri}");
            var response = await new HttpClient().GetAsync(verificationUri);

            if (outcome == HubValidationOutcome.Canceled)
            {
                return ClientValidationOutcome.NotVerified;
            }
            else
            {
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogInformation($"Status code was not success but instead {response.StatusCode}");
                    return ClientValidationOutcome.NotVerified;
                }

                var responseBody = (await response.Content.ReadAsStringAsync());
                if (responseBody != verification.Challenge)
                {
                    logger.LogInformation($"Callback result for verification request was not equal to challenge. Response body: '{responseBody}', Challenge: '{verification.Challenge}'.");
                    return ClientValidationOutcome.NotVerified;
                }

                return ClientValidationOutcome.Verified;
            }
        }
    }

    public enum HubValidationOutcome
    {
        Valid,
        Canceled,
    }

    public enum ClientValidationOutcome
    {
        Verified,
        NotVerified,
    }
}
