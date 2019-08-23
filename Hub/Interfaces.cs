using Common.Model;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FHIRcastSandbox
{
    public interface ISubscriptionValidator {
        Task<ClientValidationOutcome> ValidateSubscription(SubscriptionRequest subscription, HubValidationOutcome outcome);
    }

    public interface ISubscriptions {
        ICollection<SubscriptionRequest> GetActiveSubscriptions();
        void AddSubscription(SubscriptionRequest subscription);
        void RemoveSubscription(SubscriptionRequest subscription);
        ICollection<SubscriptionRequest> GetSubscriptions(string topic, string notificationEvent);
    }

    public interface INotifications<T> {
        Task<T> SendNotification(Notification notification, SubscriptionRequest subscription);
    }
    public interface IContexts
    {
        string addContext();
        void setContext(string topic, object context);
        object getContext(string topic);
    }
}
