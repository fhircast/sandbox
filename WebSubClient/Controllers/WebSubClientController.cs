using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FHIRcastSandbox.Model;
using FHIRcastSandbox.Model.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FHIRcastSandbox.Controllers {
    [Route("")]
    public class HomeController : Controller {
        public IActionResult Index() {
            return this.RedirectToActionPermanent(
                nameof(WebSubClientController.Get),
                nameof(WebSubClientController).Replace("Controller", ""));
        }
    }

    [Route("client")]
    public class WebSubClientController : Controller {

        private readonly ILogger<WebSubClientController> logger;
        private readonly IConfiguration config;

        #region Constructors

        public WebSubClientController(ILogger<WebSubClientController> logger, IConfiguration config) {
            this.logger = logger;
            this.config = config;
        }

        #endregion

        #region Properties

        public static ClientModel internalModel;

        private static Dictionary<string, Subscription> pendingSubs = new Dictionary<string, Subscription>();
        private static Dictionary<string, Subscription> activeSubs = new Dictionary<string, Subscription>();

        #endregion

        [HttpGet]
        public IActionResult Get() => this.View("WebSubClient", new ClientModel());

        #region Client Events

        public IActionResult Refresh() {
            if (internalModel == null) { internalModel = new ClientModel(); }

            internalModel.ActiveSubscriptions = activeSubs.Values.ToList();

            return this.View("WebSubClient", internalModel);
        }

        /// <summary>
        /// Called when the client updates its context. Lets the hub know of the changes so it
        /// can notify any subscribing apps.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("post")]
        [HttpPost]
        public IActionResult Post([FromForm] ClientModel model) {
            internalModel = model;
            var httpClient = new HttpClient();
            //var response = httpClient.PostAsync(this.Request.Scheme + "://" + this.Request.Host + "/api/hub/notify", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json")).Result;
            var response = httpClient.PostAsync(this.Request.Scheme + "://localhost:5000/api/hub/notify", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json")).Result;

            return this.View("WebSubClient", model);
        }

        #endregion

        #region Subscription events

        /// <summary>
        /// Called by hub we sent a subscription request to. They are attempting to verify the subscription.
        /// If the subscription matches one that we have sent out previously that hasn't been verified yet
        /// then return their challenge value, otherwise return a NotFound error response.
        /// </summary>
        /// <param name="subscriptionId">ID of the subscription, part of the url</param>
        /// <param name="verification">Hub's verification response to our subscription attempt</param>
        /// <returns></returns>
        [HttpGet("{subscriptionId}")]
        public IActionResult Get(string subscriptionId, [FromQuery] SubscriptionVerification hub) {
            var settingKey = "Settings:ValidateSubscriptionValidations";
            var validate = this.config.GetValue(settingKey, true);

            if (!validate) {
                this.logger.LogWarning($"Not validating subscription validation due to setting {settingKey}.");
                return this.Content(hub.Challenge);
            }

            var activeSubscription = pendingSubs.TryGetValue(subscriptionId, out var pendingSub);
            if (!activeSubscription) {
                // Received a hub request for non-pending subscription, return a NotFound response
                return this.NotFound();
            }

            // Validate hub subcription with our subscription. If a
            // match return challenge otherwise return NotFound response.
            if (SubsEqual(pendingSub, hub)) {
                // Move subscription to active sub collection and remove from pending subs
                activeSubs.Add(subscriptionId, pendingSub);
                pendingSubs.Remove(subscriptionId);
                return this.Content(hub.Challenge);
            } else {
                return this.NotFound();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        [HttpPost("{subscriptionId}")]
        public IActionResult Post(string subscriptionId, [FromBody] Notification notification) {
            //If we do not have an active subscription matching the id then return a notfound error
            if (!activeSubs.ContainsKey(subscriptionId)) { return this.NotFound(); }

            WebSubClientController.internalModel = new ClientModel()
            {
                UserIdentifier = notification.Event.Context[0] == null ? "" : notification.Event.Context[0].ToString(),
                PatientIdentifier = notification.Event.Context[1] == null ? "" : notification.Event.Context[1].ToString(),
                PatientIdIssuer = notification.Event.Context[2] == null ? "" : notification.Event.Context[2].ToString(),
                AccessionNumber = notification.Event.Context[3] == null ? "" : notification.Event.Context[3].ToString(),
                AccessionNumberGroup = notification.Event.Context[4] == null ? "" : notification.Event.Context[4].ToString(),
                StudyId = notification.Event.Context[5] == null ? "" : notification.Event.Context[5].ToString(),
            };

            return this.Ok(notification);
        }

        [Route("subscribe")]
        [HttpPost]
        public async Task<IActionResult> Subscribe(string subscriptionUrl, Uri topic, string events) {
            var eventsArray = events.Split(";", StringSplitOptions.RemoveEmptyEntries);
            var callback = this.Request.Scheme + "://" + this.Request.Host + "/client/" + Guid.NewGuid().ToString();
            var subscription = Subscription.CreateNewSubscription(subscriptionUrl, topic, eventsArray, callback);

            pendingSubs.Add(subscription.GetSubscriptionId(), subscription);
            this.logger.LogDebug($"Subscription for 'subscribing': {Environment.NewLine}{subscription}");

            var httpcontent = subscription.CreateHttpContent();

            this.logger.LogDebug($"Posting async to {subscriptionUrl}: {subscription}");
            var result = await new HttpClient().PostAsync(subscriptionUrl, httpcontent);

            if (internalModel == null) { internalModel = new ClientModel(); }
            return this.View("WebSubClient", internalModel);
        }

        [Route("unsubscribe/{subscriptionId}")]
        [HttpPost]
        public async Task<IActionResult> Unsubscribe(string subscriptionId) {
            this.logger.LogDebug($"Unsubscribing subscription {subscriptionId}");
            var exists = activeSubs.TryGetValue(subscriptionId, out var sub);
            if (!exists) { return this.View("WebSubClient", internalModel); }
            sub.Mode = SubscriptionMode.unsubscribe;

            var httpClient = new HttpClient();
            var result = await httpClient.PostAsync(sub.HubURL,
                new StringContent(
                    $"hub.callback={sub.Callback}" +
                    $"&hub.mode={sub.Mode}" +
                    $"&hub.topic={sub.Topic}" +
                    $"&hub.secret={sub.Secret}" +
                    $"&hub.events={string.Join(",", sub.Events)}" +
                    $"&hub.lease_seconds={sub.LeaseSeconds}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"));

            activeSubs.Remove(subscriptionId);

            return this.View("WebSubClient", internalModel);
        }

        #endregion

        #region Private methods

        private bool SubsEqual(SubscriptionBase sub1, SubscriptionBase sub2) {
            return sub1.Callback == sub2.Callback && sub1.Topic == sub2.Topic;
        }

        #endregion
    }
}
