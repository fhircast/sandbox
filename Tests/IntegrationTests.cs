using Common.Model;
using FHIRcastSandbox.Model.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace FHIRcastSandbox
{
    public class IntegrationTests : IDisposable
    {
        private readonly IWebHost hubServer;
        private readonly IWebHost webSubClientServer;
        private readonly int hubServerPort;
        private readonly int webSubClientServerPort;

        public IntegrationTests()
        {
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

        private IWebHost CreateWebSubClientServer(int port)
        {
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

        private IWebHost CreateHubServer(int port)
        {
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

        private int GetFreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private void VerifiySubscription(SubscriptionRequest sentSubscription, SubscriptionRequest returnedSubscription)
        {
            Assert.Equal(sentSubscription.Secret, returnedSubscription.Secret);
            Assert.Equal(sentSubscription.Mode, returnedSubscription.Mode);
            Assert.Equal(sentSubscription.Topic, returnedSubscription.Topic);
            // Server isn't setting the HubURL; should it?
            //Assert.Equal(sentSubscription.HubURL.URL, returnedSubscription.HubURL.URL);
            Assert.Equal(sentSubscription.Callback, returnedSubscription.Callback);
            Assert.Equal(sentSubscription.Lease_Seconds, returnedSubscription.Lease_Seconds);
            // Check for all events in array
            foreach (string sentEvent in sentSubscription.Events)
            {
                Assert.True(returnedSubscription.Events.Contains(sentEvent), $"Missing event: {sentEvent} from subscription");
            }
        }

        //[Fact]
        // TODO: This test only sometimes passes and seems to be a timing issue. When run only by itself it always passes, but when run with the other tests
        //       it is less reliable. Might need to refactor how these tests are created and run.
        public async Task ListingSubscriptions_AfterSubscribingToHub_ReturnsSubsription_Test()
        {
            // Arrange
            var sessionId = "some_id";
            var connectionId = "some_client_connection_id";
            var subscriptionUrl = $"http://localhost:{this.hubServerPort}/api/hub";
            var topic = $"{subscriptionUrl}/{sessionId}";
            var events = new[] { "some_event", "another_event" };
            var callback = $"http://localhost:{this.webSubClientServerPort}/callback/{connectionId}";
            int leaseSeconds = 2400;
            var subscription = CreateNewSubscription(subscriptionUrl, topic, events, callback, leaseSeconds);
            var httpContent = subscription.CreateHttpContent();

            var clientTestResponse = await new HttpClient().GetAsync(callback);
            Assert.True(clientTestResponse.IsSuccessStatusCode, $"Could not connect to web sub client: {clientTestResponse}");

            var subscriptionResponse = await new HttpClient().PostAsync(subscriptionUrl, httpContent);
            Assert.True(subscriptionResponse.IsSuccessStatusCode, $"Could not subscribe to hub: {subscriptionResponse}");
            await Task.Delay(1000);

            // Act
            var result = await new HttpClient().GetStringAsync(subscriptionUrl);
            var subscriptions = JsonConvert.DeserializeObject<SubscriptionRequest[]>(result);

            // Assert
            Assert.Single(subscriptions);

            // Check that all the passed values in subscription are as expected
            SubscriptionRequest returnedSubstription = subscriptions[0];
            VerifiySubscription(subscription, returnedSubstription);
        }

        //[Fact]
        // TODO: There are some timing issues affecting this test. When unsubscribing the call is somehow receiving the first subscribe message
        //       as well.
        public async Task ListingSubscriptions_AfterUnSubscribingFromHub_Test()
        {
            // Arrange
            var sessionId = "subscribe_id1";
            var connectionId = "subscribe_client_connection_id_1";
            var subscriptionUrl = $"http://localhost:{this.hubServerPort}/api/hub";
            var topic = $"{subscriptionUrl}/{sessionId}";
            var events = new[] { "some_event_1" };
            var callback = $"http://localhost:{this.webSubClientServerPort}/callback/{connectionId}";
            int leaseSeconds = 1201;
            var subscription1 = CreateNewSubscription(subscriptionUrl, topic, events, callback, leaseSeconds);
            var httpContent = subscription1.CreateHttpContent();

            var clientTestResponse = await new HttpClient().GetAsync(callback);
            Assert.True(clientTestResponse.IsSuccessStatusCode, $"Could not connect to web sub client: {clientTestResponse}");

            var subscriptionResponse = await new HttpClient().PostAsync(subscriptionUrl, httpContent);
            Assert.True(subscriptionResponse.IsSuccessStatusCode, $"Could not subscribe to hub: {subscriptionResponse}");
            await Task.Delay(1000);

            // Act
            var result = await new HttpClient().GetStringAsync(subscriptionUrl);
            var subscriptions = JsonConvert.DeserializeObject<SubscriptionRequest[]>(result);

            // Assert
            Assert.Single(subscriptions);
            // Check that all the passed values in subscription are as expected
            SubscriptionRequest returnedSubstription = subscriptions[0];
            VerifiySubscription(subscription1, returnedSubstription);

            // Arrange
            sessionId = "unsubscribe_id";
            connectionId = "unsubscribe_client_connection_id";
            subscriptionUrl = $"http://localhost:{this.hubServerPort}/api/hub";
            topic = $"{subscriptionUrl}/{sessionId}";
            events = new[] { "some_event" };
            callback = $"http://localhost:{this.webSubClientServerPort}/callback/{connectionId}";
            leaseSeconds = 1202;
            var subscription2 = CreateNewSubscription(subscriptionUrl, topic, events, callback, leaseSeconds);
            httpContent = subscription2.CreateHttpContent();

            clientTestResponse = await new HttpClient().GetAsync(callback);
            Assert.True(clientTestResponse.IsSuccessStatusCode, $"Could not connect to web sub client: {clientTestResponse}");

            subscriptionResponse = await new HttpClient().PostAsync(subscriptionUrl, httpContent);
            Assert.True(subscriptionResponse.IsSuccessStatusCode, $"Could not subscribe to hub: {subscriptionResponse}");
            await Task.Delay(1000);

            // Act
            result = await new HttpClient().GetStringAsync(subscriptionUrl);
            subscriptions = JsonConvert.DeserializeObject<SubscriptionRequest[]>(result);

            // Assert
            Assert.Equal(2, subscriptions.Length);

            // Check that all the passed values in both subscriptions are as expected
            returnedSubstription = subscriptions.FirstOrDefault(a => a.Topic.Equals(subscription1.Topic));
            VerifiySubscription(subscription1, returnedSubstription);

            returnedSubstription = subscriptions.FirstOrDefault(a => a.Topic.Equals(subscription2.Topic));
            VerifiySubscription(subscription2, returnedSubstription);

            //var unSubscription = CreateNewSubscription(subscriptionUrl, topic, events, callback, leaseSeconds);
            subscription2.Mode = SubscriptionMode.unsubscribe;
            httpContent = subscription2.CreateHttpContent();

            clientTestResponse = await new HttpClient().GetAsync(callback);
            Assert.True(clientTestResponse.IsSuccessStatusCode, $"Could not connect to web sub client: {clientTestResponse}");

            subscriptionResponse = await new HttpClient().PostAsync(subscriptionUrl, httpContent);
            Assert.True(subscriptionResponse.IsSuccessStatusCode, $"Could not subscribe to hub: {subscriptionResponse}");
            await Task.Delay(1000);

            // Act
            result = await new HttpClient().GetStringAsync(subscriptionUrl);
            subscriptions = JsonConvert.DeserializeObject<SubscriptionRequest[]>(result);

            // Assert
            Assert.Single(subscriptions);
            // Check that all the passed values in subscription are as expected
            returnedSubstription = subscriptions.FirstOrDefault(a => a.Topic.Equals(subscription1.Topic));
            VerifiySubscription(subscription1, returnedSubstription);
        }

        public void Dispose()
        {
            Task.WaitAll(
                this.hubServer.StopAsync(),
                this.webSubClientServer.StopAsync());
            this.hubServer.Dispose();
            this.webSubClientServer.Dispose();
        }

        private SubscriptionRequest CreateNewSubscription(string subscriptionUrl, string topic, string[] events, string callback, int leaseSeconds = 3600)
        {
            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[32];
            rngCsp.GetBytes(buffer);
            var secret = BitConverter.ToString(buffer).Replace("-", "");
            var subscription = new SubscriptionRequest()
            {
                Callback = callback,
                Events = events,
                Mode = Common.Model.SubscriptionMode.subscribe,
                Secret = secret,
                Lease_Seconds = leaseSeconds,
                Topic = topic
            };
            subscription.HubDetails = new HubDetails() { HubUrl = subscriptionUrl };

            return subscription;
        }
    }
}

