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
    public class FHIRcastClientController : Controller {

        private static ClientModel internalModel;

        [HttpGet]
        public IActionResult Get() => View("FHIRcastClient", new ClientModel());

        [HttpPost]
        public IActionResult Post([FromForm] ClientModel model) {

            internalModel = model;
            RedirectToAction("Hub", "Notify", model);

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
