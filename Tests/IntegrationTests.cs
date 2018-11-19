using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FHIRcastSandbox.Hubs;
using FHIRcastSandbox.Model.Http;
using FHIRcastSandbox.Model;
using FakeItEasy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace FHIRcastSandbox {
    public class IntegrationTests : IDisposable {
        private readonly IWebHost hubServer;
        private readonly IWebHost webSubClientServer;
        private readonly int hubServerPort;
        private readonly int webSubClientServerPort;
        private IClientProxy signalrClientProxy;

        public IntegrationTests() {
            this.hubServerPort = this.GetFreePort();
            this.hubServer = this.CreateHubServer(this.hubServerPort);
            Console.WriteLine($"Hub: http://localhost:{this.hubServerPort}");

            this.webSubClientServerPort = this.GetFreePort();
            (this.webSubClientServer, this.signalrClientProxy) = this.CreateWebSubClientServer(this.webSubClientServerPort);
            Console.WriteLine($"WebSubClient: http://localhost:{this.webSubClientServerPort}");

            Task.WaitAll(
                this.hubServer.StartAsync(),
                this.webSubClientServer.StartAsync());
            System.Threading.Thread.Sleep(1000);
        }

        private (IWebHost, IClientProxy) CreateWebSubClientServer(int port) {
            var testSignalrHubContext = A.Fake<IHubContext<WebSubClientHub>>();
            var signalrClientProxy = A.Fake<IClientProxy>();
            A.CallTo(() => testSignalrHubContext.Clients.Clients(A<string[]>._))
                .Returns(signalrClientProxy);

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "WebSubClient");
            var webHost = FHIRcastSandbox.WebSubClient.Program.CreateWebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseUrls($"http://localhost:{port}")
                .ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string> {
                        { "Settings:ValidateSubscriptionValidations", "False" },
                        { "Logging:LogLevel:Default", "Warning" },
                    }))
                .ConfigureTestServices(services => {
                    services.AddSingleton<IHubContext<WebSubClientHub>>(testSignalrHubContext);
                })
                .Build();

            return (webHost, signalrClientProxy);
        }

        private IWebHost CreateHubServer(int port) {
            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Hub");
            return FHIRcastSandbox.Program.CreateWebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseUrls($"http://localhost:{port}")
                .ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string> {
                        { "Logging:LogLevel:Default", "Warning" },
                    }))
                .Build();
        }

        private int GetFreePort() {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        [Fact]
        public async Task ListingSubscriptions_AfterSubscribingToHub_ReturnsSubsription_Test() {
            // Arrange
            var sessionId = "some_id";
            var connectionId = "some_client_connection_id";
            var subscriptionUrl = $"http://localhost:{this.hubServerPort}/api/hub";
            var topic = $"{subscriptionUrl}/{sessionId}";
            var secret = "some_secret";
            var events = new[] { "some_event" };
            var callback = $"http://localhost:{this.webSubClientServerPort}/callback/{connectionId}";
            var subscription = Subscription.CreateNewSubscription(subscriptionUrl, topic, events, callback);
            subscription.Secret = secret;
            var httpContent = subscription.CreateHttpContent();

            var clientTestResponse = await new HttpClient().GetAsync(callback);
            Assert.True(clientTestResponse.IsSuccessStatusCode, $"Could not connect to web sub client: {clientTestResponse}");

            // Subscribe to Hub
            var subscriptionResponse = await new HttpClient().PostAsync(subscriptionUrl, httpContent);
            Assert.True(subscriptionResponse.IsSuccessStatusCode, $"Could not subscribe to hub: {subscriptionResponse}");
            await Task.Delay(1000);

            // Notify Hub
            var notification = new Notification();
            notification.Event.Topic = topic;
            notification.Event.Event = events[0];
            var notificationContent = notification.CreateHttpContent();
            var notificationResult = await new HttpClient().PostAsync(topic, notificationContent);
            notificationResult.EnsureSuccessStatusCode();

            // Assert that the notificaiton was sent to the web client
            A.CallTo(() => this.signalrClientProxy.SendCoreAsync(
                        "notification",
                        A<object[]>.That.IsSameSequenceAs(new[] { notification }),
                        A<System.Threading.CancellationToken>._))
                    .MustHaveHappened();
        }

        public void Dispose() {
            Task.WaitAll(
                this.hubServer.StopAsync(),
                this.webSubClientServer.StopAsync());
            this.hubServer.Dispose();
            this.webSubClientServer.Dispose();
        }
    }
}

