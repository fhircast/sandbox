using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;

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

        private static ClientModel internalModel;

        public static ClientModel pubModel;

        

        [HttpGet]
        public IActionResult Get() => View("FHIRcastClient", new ClientModel());

        public IActionResult Refresh()
        {
            if (pubModel == null) { pubModel = new ClientModel(); }
            internalModel = pubModel;
            return View("FHIRcastClient", pubModel);
        }

        [Route("post")]
        [HttpPost]
        public IActionResult Post([FromForm] ClientModel model) {

            internalModel = model;
            var httpClient = new HttpClient();
            var response = httpClient.PostAsync(this.Request.Scheme + "://" + this.Request.Host + "/api/hub/notify", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json")).Result;

            return View("FHIRcastClient", model);
        }

        [Route("subscribe")]
        [HttpPost]
        public async Task<IActionResult> Subscribe(string subscriptionUrl, string topic, string events) {

            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[100];
            rngCsp.GetBytes(buffer);
            var secret = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var httpClient = new HttpClient();
            var data = new Subscription() {
                Callback = new Uri(this.Request.Scheme + "://" + this.Request.Host + "/api/echo"),
                Events = events.Split(";", StringSplitOptions.RemoveEmptyEntries),
                Mode = SubscriptionMode.Subscribe,
                Secret = secret,
                LeaseSeconds = 3600,
                Topic = topic
            };
            var result = await httpClient.PostAsync(subscriptionUrl, 
                new StringContent(
                    $"hub.callback={data.Callback}" +
                    $"&hub.mode={data.Mode}" +
                    $"&hub.topic={data.Topic}" +
                    $"&hub.secret={data.Secret}" +
                    $"&hub.events={string.Join(",", data.Events)}" +
                    $"&hub.lease_seconds={data.LeaseSeconds}",
                    Encoding.UTF8, 
                    "application/x-www-form-urlencoded"));

            return View("FHIRcastClient", internalModel);
        }
    }
}
