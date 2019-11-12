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

            HttpResponseMessage response = new HttpResponseMessage();
            Uri verificationUri = verification.VerificationURI();

            logger.LogDebug($"Calling callback url: {verificationUri}");
            response = await new HttpClient().GetAsync(verificationUri);

            if (outcome == HubValidationOutcome.Canceled)
            {
                return ClientValidationOutcome.NotVerified;
            }
            else
            {
                if (await ValidVerificationResponseAsync(verification, response))
                {
                    return ClientValidationOutcome.Verified;
                }
                else
                {
                    return ClientValidationOutcome.NotVerified;
                }
            }
        }

        /// <summary>
        /// Validates the subscribing apps response to our verification. Confirms a successful status code
        /// and the response matches our verification challenge.
        /// </summary>
        /// <param name="verification">Verification intent sent to subscriber</param>
        /// <param name="response">Subscriber's HTTP response message</param>
        /// <returns>true if valid, else false</returns>
        private async Task<bool> ValidVerificationResponseAsync(SubscriptionVerification verification, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation($"Status code was not success but instead {response.StatusCode}");
                return false;
            }

            var responseBody = (await response.Content.ReadAsStringAsync());
            if (responseBody != verification.Challenge)
            {
                logger.LogInformation($"Callback result for verification request was not equal to challenge. Response body: '{responseBody}', Challenge: '{verification.Challenge}'.");
                return false;
            }

            return true;
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
