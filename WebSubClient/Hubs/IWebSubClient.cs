using FHIRcastSandbox.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Hubs
{
    /// <summary>
    /// Interface to create a strongly typed SignalR hub between WebSubClientHub and the javascript client
    /// </summary>
    public interface IWebSubClient
    {
        Task ReceivedNotification(Notification notification);

        Task AddSubscription(SubscriptionWithHubURL subscription);
        Task AddSubscriber(Subscription subscription);

        Task AlertMessage(string message);
    }
}
