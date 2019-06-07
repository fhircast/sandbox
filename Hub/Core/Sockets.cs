using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Hub.Core
{
    public class Sockets
    {
        private readonly ILogger logger = null;
        private readonly ISubscriptions subscriptions;
        private readonly INotifications<HttpResponseMessage> notifications;
        private List<WebSocket> sockets = new List<WebSocket>();

        public Sockets(ISubscriptions subscriptions, ILogger<Sockets> logger, INotifications<HttpResponseMessage> notifications)
        {
            this.logger = logger;
            this.subscriptions = subscriptions;
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        }


        public async Task AddSocket(WebSocket socket)
        {
            sockets.Add(socket);
            logger.LogDebug($"Adding socket: {socket.GetHashCode()}");
            await HandleSocketMessageAsync(socket);
        }

        public async Task HandleSocketMessageAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                //Console.WriteLine($"Buffer to string: {Encoding.Default.GetString(buffer).Substring(0,result.Count)} from {webSocket.GetHashCode()}");
                logger.LogDebug($"Buffer to string: {Encoding.Default.GetString(buffer).Substring(0, result.Count)} from {webSocket.GetHashCode()}");
                //await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                ParseSocketMessage(webSocket, Encoding.Default.GetString(buffer).Substring(0, result.Count));
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private async void ParseSocketMessage(WebSocket socket, string message)
        {
            try
            {
                JObject jObject = JObject.Parse(message);
                string action = (string)jObject.SelectToken("action");

                switch (action)
                {
                    case "event":
                        logger.LogDebug($"Received event action from socket {socket.GetHashCode()}");
                        JToken context = jObject.SelectToken("context");
                        logger.LogDebug($"Received context: {context.ToString()}");
                        var subscriptionList = subscriptions.GetSubscriptions((string)context.SelectToken("topic"), (string)context.SelectToken("event"));

                        if (subscriptionList.Count != 0)
                        {
                            Notification notification = new Notification()
                            {
                                Timestamp = DateTime.Now,
                                Id = "",
                                Event = new NotificationEvent
                                {
                                    Topic = (string)context.SelectToken("topic"),
                                    Event = (string)context.SelectToken("event"),
                                    Context = new Object[]
                                    {
                                        "test"
                                    }
                                }
                            };

                            var success = true;
                            foreach (var sub in subscriptionList)
                            {
                                await this.notifications.SendNotification(notification, sub);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error parsing message on socket {socket.GetHashCode()}: {ex.Message}");
            }
            
        }

    }
}
