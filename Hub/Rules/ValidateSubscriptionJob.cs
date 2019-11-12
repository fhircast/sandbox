using Common.Model;
using FHIRcastSandbox.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Rules
{
    public class ValidateSubscriptionJob
    {
        private readonly ISubscriptionValidator validator;
        private readonly ISubscriptions subscriptions;
        private readonly ILogger<ValidateSubscriptionJob> logger;
        private readonly InternalHub internalHub;

        public ValidateSubscriptionJob(ISubscriptionValidator validator, ISubscriptions subscriptions, ILogger<ValidateSubscriptionJob> logger, InternalHub internalHub)
        {
            this.validator = validator;
            this.subscriptions = subscriptions;
            this.logger = logger;
            this.internalHub = internalHub;
        }

        public async Task Run(SubscriptionRequest subscription, bool simulateCancellation)
        {
            HubValidationOutcome validationOutcome = simulateCancellation ? HubValidationOutcome.Canceled : HubValidationOutcome.Valid;
            ClientValidationOutcome validationResult;

            // Shouldn't have a websocket subscription here, we only need to validate webhook
            if (subscription.Channel.Type == SubscriptionChannelType.websocket)
            {
                validationResult = ClientValidationOutcome.Verified;
                return;
            }
            else
            {
                validationResult = await validator.ValidateSubscription(subscription, validationOutcome);
            }            

            if (validationResult == ClientValidationOutcome.Verified)
            {
                if (subscription.Mode == SubscriptionMode.subscribe)
                {
                    // Add subscription to collection and inform client
                    logger.LogInformation($"Adding verified subscription: {subscription}.");
                    subscriptions.ActivatePendedSubscription(subscription.Callback);
                    await internalHub.NotifyClientOfSubscriber(subscription.Topic, subscription);
                }
                else if (subscription.Mode == SubscriptionMode.unsubscribe)
                {
                    logger.LogInformation($"Removing verified subscription: {subscription}.");
                    subscriptions.RemoveSubscription(subscription);
                }
            }
            else
            {
                var addingOrRemoving = subscription.Mode == SubscriptionMode.subscribe ? "adding" : "removing";
                logger.LogInformation($"Not {addingOrRemoving} unverified subscription: {subscription}.");
            }
        }
    }
}
