using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace FHIRcastSandbox.Hubs {
    public class ClientHub : Hub {
        public override Task OnConnectedAsync() {
            var caller = this.Clients.Caller;
            Task.Delay(2000).ContinueWith(delegate { caller.SendAsync("ReceiveMessage", "hello there from the server"); });
            return base.OnConnectedAsync();
        }
    }
}