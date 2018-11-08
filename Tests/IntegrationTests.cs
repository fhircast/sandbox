using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Model.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace FHIRcastSandbox {
    public class IntegrationTests : IDisposable {
        private readonly IWebHost hubServer;
        private readonly IWebHost webSubClientServer;
        private readonly int hubServerPort;
        private readonly int webSubClientServerPort;

        public IntegrationTests() {
            this.hubServerPort = this.GetFreePort();
            this.hubServer = this.CreateHubServer(this.hubServerPort);
            Console.WriteLine($"Hub: http://localhost:{this.hubServerPort}");

            this.webSubClientServerPort = this.GetFreePort();
            this.webSubClientServer = this.CreateWebSubClientServer(this.webSubClientServerPort);
            Console.WriteLine($"WebSubClient: http://localhost:{this.webSubClientServerPort}");

            Task.WaitAll(
                this.hubServer.StartAsync(),
                this.webSubClientServer.StartAsync());
            System.Threading.Thread.Sleep(1000);
        }

        private IWebHost CreateWebSubClientServer(int port) {
            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "WebSubClient");
            return FHIRcastSandbox.WebSubClient.Program.CreateWebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseUrls($"http://localhost:{port}")
                .ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string> {
                        { "Settings:ValidateSubscriptionValidations", "False" },
                        { "Logging:LogLevel:Default", "Warning" },
                    }))
                .Build();
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
            var events = new[] { "some_event" };
            var callback = $"http://localhost:{this.webSubClientServerPort}/callback/{connectionId}";
            var subscription = Subscription.CreateNewSubscription(subscriptionUrl, topic, events, callback);
            var httpContent = subscription.CreateHttpContent();

            var clientTestResponse = await new HttpClient().GetAsync(callback);
            Assert.True(clientTestResponse.IsSuccessStatusCode, $"Could not connect to web sub client: {clientTestResponse}");

            var subscriptionResponse = await new HttpClient().PostAsync(subscriptionUrl, httpContent);
            Assert.True(subscriptionResponse.IsSuccessStatusCode, $"Could not subscribe to hub: {subscriptionResponse}");
            await Task.Delay(1000);

            // Act
            var result = await new HttpClient().GetStringAsync(subscriptionUrl);
            var subscriptions = JsonConvert.DeserializeObject<Subscription[]>(result);

            // Assert
            Assert.Single(subscriptions);
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

