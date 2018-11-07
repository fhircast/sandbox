using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FHIRcastSandbox.Model.Http;
using FHIRcastSandbox.Model;
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
            this.UID = Guid.NewGuid().ToString("n");
            this.config = config;
        }

        #endregion

        #region Properties

        public static ClientModel internalModel;

        private static Dictionary<string, Subscription> pendingSubs = new Dictionary<string, Subscription>();
        private static Dictionary<string, Subscription> activeSubs = new Dictionary<string, Subscription>();

        public string UID { get; set; }

        #endregion

        [HttpGet]
        public IActionResult Get() => View("WebSubClient", new ClientModel());

        #region Client Events

        public IActionResult Refresh() {
            if (internalModel == null) { internalModel = new ClientModel(); }

            internalModel.ActiveSubscriptions = activeSubs.Values.ToList();

            return View("WebSubClient", internalModel);
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

            return View("WebSubClient", model);
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
            var validate = this.config.GetValue<bool>(settingKey, true);

            if (!validate) {
                this.logger.LogWarning($"Not validating subscription validation due to setting {settingKey}.");
                return this.Content(hub.Challenge);
            }

            var activeSubscription = pendingSubs.ContainsKey(subscriptionId);
            if (!activeSubscription) {
                // Received a hub request for non-pending subscription, return a NotFound response
                return NotFound();
            }

            Subscription sub = pendingSubs[subscriptionId];

            // Validate hub subcription with our subscription. If a
            // match return challenge otherwise return NotFound response.
            if (SubsEqual(sub, hub)) {
                //Move subscription to active sub collection and remove from pending subs
                activeSubs.Add(subscriptionId, sub);
                pendingSubs.Remove(subscriptionId);
                return this.Content(hub.Challenge);
            } else {
                return NotFound();
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
            if (!activeSubs.ContainsKey(subscriptionId)) { return NotFound(); }

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
        public async Task<IActionResult> Subscribe(string subscriptionUrl, string topic, string events) {
            var eventsArray = events.Split(";", StringSplitOptions.RemoveEmptyEntries);
            var subscriptionId = Guid.NewGuid().ToString("n");
            var callback = this.Request.Scheme + "://" + this.Request.Host + "/client/" + subscriptionId;
            var subscription = Subscription.CreateNewSubscription(subscriptionId, subscriptionUrl, topic, eventsArray, callback);

            pendingSubs.Add(subscription.UID, subscription);

            this.logger.LogDebug($"Subscription for 'subscribing': {Environment.NewLine}{subscription}");

            var httpcontent = subscription.CreateHttpContent();

            this.logger.LogDebug($"Posting async to {subscriptionUrl}: {subscription}");

            var result = await new HttpClient().PostAsync(subscriptionUrl, httpcontent);

            if (internalModel == null) { internalModel = new ClientModel(); }
            return View("WebSubClient", internalModel);
        }

        [Route("unsubscribe/{subscriptionId}")]
        [HttpPost]
        public async Task<IActionResult> Unsubscribe(string subscriptionId) {
            this.logger.LogDebug($"Unsubscribing subscription {subscriptionId}");
            if (!activeSubs.ContainsKey(subscriptionId)) { return View("WebSubClient", internalModel); }
            Subscription sub = activeSubs[subscriptionId];
            sub.Mode = SubscriptionMode.unsubscribe;

            var httpClient = new HttpClient();
            var result = await httpClient.PostAsync(sub.HubURL,
                new StringContent(
                    $"hub.callback={sub.Callback}" +
                    $"&hub.mode={sub.Mode}" +
                    $"&hub.topic={sub.Topic}" +
                    $"&hub.secret={sub.Secret}" +
                    $"&hub.events={string.Join(",", sub.Events)}" +
                    $"&hub.lease_seconds={sub.LeaseSeconds}" +
                    $"&hub.UID={sub.UID}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"));

            activeSubs.Remove(subscriptionId);

            return View("WebSubClient", internalModel);
        }
        #endregion

        #region Private methods
        private bool SubsEqual(SubscriptionBase sub1, SubscriptionBase sub2) {
            return sub1.Callback == sub2.Callback && sub1.Topic == sub2.Topic;
        }
        #endregion
    }
}
