using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Rules;

namespace FHIRcastSandbox {
    public interface ISubscriptionValidator {
        Task<ClientValidationOutcome> ValidateSubscription(Subscription subscription, HubValidationOutcome outcome);
    }

    public interface ISubscriptions {
        ICollection<Subscription> GetActiveSubscriptions();
        void AddSubscription(Subscription subscription);
        void RemoveSubscription(Subscription subscription);
        ICollection<Subscription> GetSubscriptions(Uri topic, string notificationEvent);
    }

    public interface INotifications {
        Task SendNotification(Notification notification, Subscription subscription);
    }
}
