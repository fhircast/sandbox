using System.Threading.Tasks;
using FHIRcastSandbox.Model;

namespace FHIRcastSandbox.WebSubClient.Rules {
    public interface IHubSubscriptions {
        Task SubscribeAsync(Subscription subscription);
        Task Unsubscribe(Subscription sub);
    }
}