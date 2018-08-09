using FHIRcastSandbox.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace FHIRcastSandbox.Rules {
    public class ValidateSubscriptionJob {
        private readonly ISubscriptionValidator validator;
        private readonly ISubscriptions subscriptions;
        private readonly ILogger<ValidateSubscriptionJob> logger;

        public ValidateSubscriptionJob(ISubscriptionValidator validator, ISubscriptions subscriptions, ILogger<ValidateSubscriptionJob> logger) {
            this.validator = validator;
            this.subscriptions = subscriptions;
            this.logger = logger;
        }

        public async Task Run(Subscription subscription, bool simulateCancellation) {
            if (subscription.Mode == SubscriptionMode.subscribe) {
                HubValidationOutcome validationOutcome = simulateCancellation ? HubValidationOutcome.Canceled : HubValidationOutcome.Valid;
                var validationResult = await this.validator.ValidateSubscription(subscription, validationOutcome);
                if (validationResult == ClientValidationOutcome.Verified) {
                    this.logger.LogInformation($"Adding verified subscription: {subscription}.");
                    this.subscriptions.AddSubscription(subscription);
                } else {
                    this.logger.LogInformation($"Not adding unverified subscription: {subscription}.");
                }
            } else if (subscription.Mode == SubscriptionMode.unsubscribe) {
                this.subscriptions.RemoveSubscription(subscription);
            }
        }
    }
}
