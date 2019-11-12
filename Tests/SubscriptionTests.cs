using Common.Model;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web;
using Xunit;

namespace FHIRcastSandbox
{
    public class SubscriptionTests
    {
        private readonly TestServer _hubServer;
        private readonly HttpClient _hubClient;

        private readonly TestServer _webSubServer;
        private readonly HttpClient _webSubClient;

        public SubscriptionTests()
        {
            _hubServer = new TestServer(WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseEnvironment("Development"));
            _hubClient = _hubServer.CreateClient();

            _webSubServer = new TestServer(WebHost.CreateDefaultBuilder()
                .UseStartup<WebSubClient.Startup>()
                .UseEnvironment("Development")
                .ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string> {
                        { "Settings:ValidateSubscriptionValidations", "False" },
                        { "Logging:LogLevel:Default", "Warning" },
                    })));


            _webSubClient = _webSubServer.CreateClient();
        }

        #region Model Tests
        [Fact]
        public void SubscriptionVerification_VerificationURI_ReturnsCorrectURI()
        {
            string callback = "https://testcallback/callback";
            SubscriptionMode mode = SubscriptionMode.subscribe;
            string topic = "testTopic";
            string secret = "secretCode";
            string[] events = new string[] { "open-patient", "close-patient" };
            int lease_seconds = 3600;

            SubscriptionRequest subscriptionRequest = new SubscriptionRequest()
            {
                Callback = callback,
                Mode = mode,
                Topic = topic,
                Secret = secret,
                Events = events,
                Lease_Seconds = lease_seconds
            };

            SubscriptionVerification subscriptionVerification = SubscriptionVerification.CreateSubscriptionVerification(subscriptionRequest);

            string verificationURL = $"{callback}?hub.mode={HttpUtility.UrlEncode(mode.ToString())}"
                + $"&hub.topic={HttpUtility.UrlEncode(topic)}&hub.events={HttpUtility.UrlEncode(string.Join(",", events))}"
                + $"&hub.lease_seconds={HttpUtility.UrlEncode(lease_seconds.ToString())}&hub.challenge={HttpUtility.UrlEncode(subscriptionVerification.Challenge)}";

            Assert.Equal(verificationURL, subscriptionVerification.VerificationURI().ToString());
        }

        [Fact]
        public void EqualSubscriptionRequests_Equals_ReturnsTrue()
        {
            SubscriptionRequest request1 = new SubscriptionRequest()
            {
                Callback = "callback",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Secret = "secret",
                Topic = "topic"
            };
            SubscriptionRequest request2 = new SubscriptionRequest()
            {
                Callback = "callback",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Secret = "secret",
                Topic = "topic"
            };

            Assert.True(request1.Equals(request2));
        }

        [Fact]
        public void UnequalSubscriptionRequests_Equals_ReturnsFalse()
        {
            SubscriptionRequest request1 = new SubscriptionRequest()
            {
                Callback = "callback",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Secret = "secret",
                Topic = "topic"
            };
            SubscriptionRequest request2 = new SubscriptionRequest()
            {
                Callback = "invalid callback",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Secret = "secret",
                Topic = "topic"
            };

            Assert.False(request1.Equals(request2));    // Unequal callback

            request2.Callback = request1.Callback;
            request2.Events = new string[] { "event1", "event2", "event3" };

            Assert.False(request1.Equals(request2));    // Unequal events

            request2.Events = request1.Events;
            request2.Topic = "invalid topic";

            Assert.False(request1.Equals(request2));    // Unequal topic
        }

        [Fact]
        public void EqualSubscriptionVerifications_Equals_ReturnsTrue()
        {
            SubscriptionVerification verification1 = new SubscriptionVerification()
            {
                Challenge = "challenge",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Topic = "topic"
            };
            SubscriptionVerification verification2 = new SubscriptionVerification()
            {
                Challenge = "challenge",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Topic = "topic"
            };

            Assert.True(verification1.Equals(verification2));
        }

        [Fact]
        public void UnequalSubscriptionVerifications_Equals_ReturnsFalse()
        {
            SubscriptionVerification verification1 = new SubscriptionVerification()
            {
                Challenge = "challenge",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Topic = "topic"
            };
            SubscriptionVerification verification2 = new SubscriptionVerification()
            {
                Challenge = "challenge",
                Events = new string[]
                {
                    "event1",
                    "event2",
                    "event3"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Topic = "topic"
            };

            Assert.False(verification1.Equals(verification2));

            verification2.Events = verification1.Events;
            verification2.Topic = "invalid topic";

            Assert.False(verification1.Equals(verification2));
        }

        [Fact]
        public void SubscriptionRequest_SubscriptionVerification_EqualCases_ReturnTrue()
        {
            // There are certain cases where we will have a SubscriptionVerification and need to match
            // it to an existing SubscriptionRequest (see Subscriptions class in WebSubClient). These
            // tests verify those cases

            SubscriptionRequest request = new SubscriptionRequest()
            {
                Callback = "callback",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Secret = "secret",
                Topic = "topic"
            };

            SubscriptionVerification verification = new SubscriptionVerification()
            {
                Challenge = "challenge",
                Events = new string[]
                {
                    "event1",
                    "event2"
                },
                Lease_Seconds = 3600,
                Mode = SubscriptionMode.subscribe,
                Topic = "topic"
            };

            Assert.True(request.Equals(verification));
            Assert.True(verification.Equals(request));
        }
        #endregion

        #region HTTP Tests
        [Fact]
        public async void Post_HubController_FromForm_ValidData_NotFoundResponse()
        {
            Dictionary<string, string> formData = new Dictionary<string, string>
            {
                {"hub.callback", "testcallback" },
                {"hub.channel.type", "webhook" },
                {"hub.mode", "subscribe" },
                {"hub.topic", "testtopic" },
                {"hub.events", "patient-open,patient-close" },
                {"hub.secret", "testsecret" },
                {"hub.lease_seconds", "3600" }
            };

            var response = await _hubClient.PostAsync("api/hub", new FormUrlEncodedContent(formData));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async void Post_HubController_FromForm_InvalidData_BadResultResponse()
        {
            Dictionary<string, string> formData = new Dictionary<string, string>
            {
                //{"hub.callback", "testcallback" },
                {"hub.mode", "subscribe" },
                {"hub.topic", "testtopic" },
                {"hub.events", "patient-open,patient-close" },
                {"hub.secret", "testsecret" },
                {"hub.lease_seconds", "3600" }
            };

            var response = await _hubClient.PostAsync("api/hub", new FormUrlEncodedContent(formData));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async void Get_CallbackController_Verification_ReturnsChallenge()
        {
            SubscriptionRequest subscription = new SubscriptionRequest()
            {
                Callback = "http://localhost:5001/callback/testTopic",
                Mode = SubscriptionMode.subscribe,
                Topic = "testTopic",
                Events = new string[]
                {
                    "patient-open",
                    "patient-close"
                },
                Secret = "testSecret",
                Lease_Seconds = 3600
            };
            SubscriptionVerification verification = SubscriptionVerification.CreateSubscriptionVerification(subscription, false);

            Uri verificationUri = verification.VerificationURI();

            var response = await _webSubClient.GetAsync(verificationUri);

            Assert.True(response.IsSuccessStatusCode);
        }
        #endregion
    }
}
