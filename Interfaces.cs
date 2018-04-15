using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace FHIRcastSandbox {
    public interface ISubscriptionValidator {
        Task<ClientValidationOutcome> ValidateSubscription(Subscription subscription, HubValidationOutcome outcome);
    }

    public interface ISubscriptions {
        ImmutableList<Subscription> GetActiveSubscriptions();
        void AddSubscription(Subscription subscription);
        void RemoveSubscription(Subscription subscription);
    }
}
