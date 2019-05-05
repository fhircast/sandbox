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

        public async Task<Boolean> Run(Subscription subscription, bool simulateCancellation) {
            HubValidationOutcome validationOutcome = simulateCancellation ? HubValidationOutcome.Canceled : HubValidationOutcome.Valid;
            var validationResult = await this.validator.ValidateSubscription(subscription, validationOutcome);
            if (validationResult == ClientValidationOutcome.Verified)
            {
                if (subscription.Mode == SubscriptionMode.subscribe)
                {
                    this.logger.LogInformation($"Adding verified subscription: {subscription}.");
                    this.subscriptions.AddSubscription(subscription);
                    return true;
                }
                else if (subscription.Mode == SubscriptionMode.unsubscribe)
                {
                    this.logger.LogInformation($"Removing verified subscription: {subscription}.");
                    this.subscriptions.RemoveSubscription(subscription);
                    return true;
                }
            }
            else
            {
                var addingOrRemoving = subscription.Mode == SubscriptionMode.subscribe ? "adding" : "removing";
                this.logger.LogInformation($"Not {addingOrRemoving} unverified subscription: {subscription}.");
                return false;
            }

            return false;
        }
    }
}
