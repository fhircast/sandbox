using Common.Model;
using FHIRcastSandbox.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Hubs
{
    /// <summary>
    /// Interface to create a strongly typed SignalR hub between WebSubClientHub and the javascript client
    /// </summary>
    public interface IWebSubClient
    {
        Task ReceivedNotification(Notification notification);

        Task SubscriptionsChanged(List<SubscriptionRequest> subscriptions);
        Task SubscriberAdded(SubscriptionRequest subscriber);
        Task SubscriberRemoved(SubscriptionRequest subscriber);

        Task AlertMessage(string message);
    }
}
