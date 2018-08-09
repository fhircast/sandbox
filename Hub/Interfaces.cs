using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace FHIRcastSandbox {
    public interface ISubscriptionValidator {
        Task<ClientValidationOutcome> ValidateSubscription(Subscription subscription, HubValidationOutcome outcome);
    }

    public interface ISubscriptions {
        ICollection<Subscription> GetActiveSubscriptions();
        void AddSubscription(Subscription subscription);
        void RemoveSubscription(Subscription subscription);
        ICollection<Subscription> GetSubscriptions(string topic, string notificationEvent);
    }

    public interface INotifications {
        Task SendNotification(Notification notification, Subscription subscription);
    }
}
