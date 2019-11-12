using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Model;
using FHIRcastSandbox.Core;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Rules
{
    public class WebSocketMiddleware : IMiddleware
    {
        private readonly ISubscriptions subscriptions;
        private readonly ILogger<WebSocketMiddleware> logger;
        private readonly string webSocketProtocol;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly ISubscriptionValidator validator;
        private readonly InternalHub internalHub;

        public WebSocketMiddleware(ILogger<WebSocketMiddleware> logger, ISubscriptions subscriptions, IBackgroundJobClient backgroundJobClient, ISubscriptionValidator validator, InternalHub internalHub)
        {
            this.subscriptions = subscriptions;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            this.validator = validator;
            this.internalHub = internalHub;
#if DEBUG
            this.webSocketProtocol = "ws://";
#else
			this.webSocketProtocol = "wss://";
#endif
        }

        /// <summary>
        /// Websocket connection handler
        /// </summary>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await next(context);
            }
            else
            {
                string key = $"{webSocketProtocol}{context.Request.Host}" +
                    $"{context.Request.PathBase}{ context.Request.Path.Value}";

                WebSocket webSocket;
                if (subscriptions.ActivatePendedSubscription(key, out SubscriptionRequest subscription))
                {
                    webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    subscription.Websocket = webSocket;

                    await internalHub.NotifyClientOfSubscriber(subscription.Topic, subscription);
                }
                else
                {
                    return;
                }

                while (webSocket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
                {
                    //There isn't anything we receive back on the websocket currently 
                    //(in this case we are the hub so we only send out notifications over the websocket)
                    #region Read from websocket
                    //await webSocketConnections.ReceiveStringAsync(webSocket);
                    await ReceiveStringAsync(webSocket);

                    string socketdata = null;
                    try
                    {
                        socketdata = await ReceiveStringAsync(webSocket); //await webSocketConnections.ReceiveStringAsync(webSocket);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"An exception occurred reading from web socket {subscription.WebsocketURL}:{Environment.NewLine}" +
                            $"{ex.Message}");
                    }
                    #endregion

                    #region Websocket close
                    if (webSocket.State != WebSocketState.Open)
                    {
                        logger.LogInformation($"Websocket {subscription.WebsocketURL} no longer open. state is {webSocket.State.ToString()}");
                        // gracefully handle aborted and intionally terminated websocket conections
                        if (webSocket.State == WebSocketState.CloseReceived)
                        {
                            logger.LogDebug($"websocket closing...");
                            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "client requesting close", new CancellationToken());
                            logger.LogDebug($"websocket closed");
                        }
                    } 
                    #endregion
                }
            }
        }

        private async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    result = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    return null;
                }
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    string data = await reader.ReadToEndAsync();
                    System.Diagnostics.Debug.WriteLine($"received data: {data}");
                    return data;
                }
            }
        }
    }
}
