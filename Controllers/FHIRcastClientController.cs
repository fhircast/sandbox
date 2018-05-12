using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Controllers {
    [Route("")]
    public class HomeController : Controller {
        public IActionResult Index() {
            return this.RedirectToActionPermanent(
                nameof(FHIRcastClientController.Get),
                nameof(FHIRcastClientController).Replace("Controller", ""));
        }
    }

    [Route("client")]
    public class FHIRcastClientController : Controller {

        private readonly ILogger<FHIRcastClientController> logger;
        private readonly ISubscriptions subscriptions;

        #region Constructors
        public FHIRcastClientController(ILogger<FHIRcastClientController> logger, ISubscriptions subscriptions)
        {
            this.logger = logger;
            this.subscriptions = subscriptions;
            this.UID = Guid.NewGuid().ToString("n");
        } 
        #endregion

        #region Properties
        public static ClientModel internalModel;

        private static Dictionary<string, Subscription> pendingSubs = new Dictionary<string, Subscription>();
        private static Dictionary<string, Subscription> activeSubs = new Dictionary<string, Subscription>();

        public string UID { get; set; }
        #endregion

        [HttpGet]
        public IActionResult Get() => View("FHIRcastClient", new ClientModel());

        #region Client Events
        public IActionResult Refresh()
        {
            if (internalModel == null) { internalModel = new ClientModel(); }

            internalModel.ActiveSubscriptions = activeSubs.Values.ToList();

            return View("FHIRcastClient", internalModel);
        }

        /// <summary>
        /// Called when the client updates its context. Lets the hub know of the changes so it
        /// can notify any subscribing apps.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("post")]
        [HttpPost]
        public IActionResult Post([FromForm] ClientModel model)
        {

            internalModel = model;
            var httpClient = new HttpClient();
            var response = httpClient.PostAsync(this.Request.Scheme + "://" + this.Request.Host + "/api/hub/notify", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json")).Result;

            return View("FHIRcastClient", model);
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
        public IActionResult Get(string subscriptionId)
        {
            if (!Request.Query.ContainsKey("hub.challenge"))
            {
                this.logger.LogDebug($"Missing hub.challenge");
                return NotFound();
            }

            if (!Request.Query.ContainsKey("hub.topic"))
            {
                this.logger.LogDebug($"Missing hub.topic");
                return NotFound();
            }

            string challenge = Request.Query["hub.challenge"];
            string topic = Request.Query["hub.topic"];

            //Received a verification request for non-pending subscription, return a NotFound response
            if (!pendingSubs.ContainsKey(subscriptionId)) { return NotFound(); }

            Subscription sub = pendingSubs[subscriptionId];

            //Validate verification subcription with our subscription. If a match return challenge
            //otherwise return NotFound response.
            if (topic == sub.Topic)
            {
                //Move subscription to active sub collection and remove from pending subs
                activeSubs.Add(subscriptionId, sub);
                pendingSubs.Remove(subscriptionId);
                return this.Content(challenge);
            }
            else
            {
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
        public IActionResult Post(string subscriptionId, [FromBody] Notification notification)
        {
            //If we do not have an active subscription matching the id then return a notfound error
            if (!activeSubs.ContainsKey(subscriptionId)) { return NotFound(); }

            FHIRcastClientController.internalModel = new ClientModel()
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

            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[100];
            rngCsp.GetBytes(buffer);
            var secret = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var httpClient = new HttpClient();
            string subUID = Guid.NewGuid().ToString("n");
            var data = new Subscription() {
                UID = subUID,
                Callback = new Uri(this.Request.Scheme + "://" + this.Request.Host + "/client/" + subUID),
                Events = events.Split(";", StringSplitOptions.RemoveEmptyEntries),
                Mode = SubscriptionMode.subscribe,
                Secret = secret,
                LeaseSeconds = 3600,
                Topic = topic
            };
            data.HubURL = subscriptionUrl;
            pendingSubs.Add(subUID, data);

            data.LogSubscriptionInfo(this.logger, "subscribing");

            string content = $"hub.callback={data.Callback}" +
                    $"&hub.mode={data.Mode}" +
                    $"&hub.topic={data.Topic}" +
                    $"&hub.secret={data.Secret}" +
                    $"&hub.events={string.Join(",", data.Events)}" +
                    $"&hub.lease_seconds={data.LeaseSeconds}" +
                    $"&hub.UID={data.UID}";

            StringContent httpcontent = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

            this.logger.LogDebug($"Posting async to {subscriptionUrl}: {content}");

            var result = await httpClient.PostAsync(subscriptionUrl, httpcontent);

            if (internalModel == null) { internalModel = new ClientModel(); }
            return View("FHIRcastClient", internalModel);
        }

        [Route("unsubscribe/{subscriptionId}")]
        [HttpPost]
        public async Task<IActionResult> Unsubscribe(string subscriptionId)
        {
            this.logger.LogDebug($"Unsubscribing subscription {subscriptionId}");
            if (!activeSubs.ContainsKey(subscriptionId)) { return View("FHIRcastClient", internalModel); }
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

            return View("FHIRcastClient", internalModel);
        }
        #endregion

        #region Private methods
        private bool SubsEqual(SubscriptionBase sub1, SubscriptionBase sub2)
        {
            return sub1.Callback == sub2.Callback && sub1.Topic == sub2.Topic;
        }
        #endregion
    }
}
