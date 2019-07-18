using FHIRcastSandbox.Model;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Core
{
    /// <summary>
    /// Interface to create a strongly typed SignalR hub from InternalHub to InternalHubClient (these are the messages
    /// the Hub project would need to send to the WebSubClient project, mainly about subscriptions received for a
    /// specific client).
    /// </summary>
    public interface IInternalHubClient
    {
        Task AddSubscriber(Subscription subscription);
        Task RemoveSubscriber(Subscription subscription);
    }
}
