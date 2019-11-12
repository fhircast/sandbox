using Common.Model;
using FHIRcastSandbox.Rules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FHIRcastSandbox
{
    public interface ISubscriptionValidator {
        Task<ClientValidationOutcome> ValidateSubscription(SubscriptionRequest subscription, HubValidationOutcome outcome);
    }

    public interface ISubscriptions {
        ICollection<SubscriptionRequest> GetPendingSubscriptions();
        ICollection<SubscriptionRequest> GetActiveSubscriptions();

        void RemoveSubscription(SubscriptionRequest subscription);
        ICollection<SubscriptionRequest> GetSubscriptions(string topic, string notificationEvent);
        void AddPendingSubscription(SubscriptionRequest subscription, string key);
        bool ActivatePendedSubscription(string key, out SubscriptionRequest subscription);
        bool ActivatePendedSubscription(string key);
        bool UnsubscribeSubscription(string key);

        
    }

    public interface IContexts
    {
        string addContext();
        void setContext(string topic, object context);
        object getContext(string topic);
    }
}
