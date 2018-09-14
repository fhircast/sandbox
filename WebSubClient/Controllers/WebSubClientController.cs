using FHIRcastSandbox.Model;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Controllers
{
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

        

        #region Constructors
        public WebSubClientController(ILogger<WebSubClientController> logger) {
            this.logger = logger;
            this.UID = Guid.NewGuid().ToString("n");

            if (internalModel == null)
            {
                internalModel = new ClientModel();
                internalModel.FHIRServer = DEFAULT_FHIR_SERVER;
                createFHIRClient();
            }           
        }

        private void createFHIRClient(string fhirServer = DEFAULT_FHIR_SERVER)
        {
            client = new FhirClient(fhirServer);
            client.PreferredFormat = ResourceFormat.Json;
        }

        
        #endregion

        #region Properties
        public static ClientModel internalModel;

        private static Dictionary<string, Model.Subscription> pendingSubs = new Dictionary<string, Model.Subscription>();
        private static Dictionary<string, Model.Subscription> activeSubs = new Dictionary<string, Model.Subscription>();

        private readonly ILogger<WebSubClientController> logger;
        private static FhirClient client;
        private static Patient _patient;
        private static ImagingStudy _study;
        const string DEFAULT_FHIR_SERVER = "http://test.fhir.org/r3";
        private const string VIEW_NAME = "WebSubClient";

        public string UID { get; set; }
        #endregion

        [HttpGet]
        public IActionResult Get()
        {         
            return View(VIEW_NAME, internalModel);
        }

        #region Client Events
        public IActionResult Refresh() {
            if (internalModel == null) { internalModel = new ClientModel(); }

            internalModel.ActiveSubscriptions = activeSubs.Values.ToList();
            return View(VIEW_NAME, internalModel);
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

            return View(VIEW_NAME, model);
        }

        [Route("searchPatients")]
        [HttpPost]
        public IActionResult SearchPatients(string patientID, string patientName)
        {
            SearchParams pars = new SearchParams();
            if (patientID != null) { pars.Add("_id", patientID); }
            if (patientName != null) { pars.Add("name", patientName); }

            Bundle bundle = client.Search<Patient>(pars);

            List<SelectListItem> patients = new List<SelectListItem>();
            foreach (Bundle.EntryComponent entry in bundle.Entry)
            {
                Patient pat = (Patient)entry.Resource;
                patients.Add(new SelectListItem
                {
                    Value = pat.Id,
                    Text = pat.Name[0].ToString()
                });
            }

            internalModel.SearchPatients = new SelectList(patients, "Value", "Text");

            return View(VIEW_NAME, internalModel);
        }

        [Route("openPatient")]
        [HttpPost]
        public IActionResult OpenPatient(string patientSelect)
        {
            if (internalModel == null) { internalModel = new ClientModel(); }
            if (patientSelect != null)
            {
                _patient = GetPatient(patientSelect);
                if (_patient != null)
                {                   
                    _study = null;
                    ClearPatientInfo();
                    return UpdateClientModel();
                }             
            }
            else
            {
                internalModel.PatientOpenErrorDiv = "<div><p>No patient ID given.</p></div>";
            }

            return View(VIEW_NAME, internalModel);
        }

        [Route("openStudy")]
        [HttpPost]
        public IActionResult OpenStudy(string studyID)
        {
            if (internalModel == null) { internalModel = new ClientModel(); }
            if (studyID != null)
            {
                _study = GetStudy(studyID);
                if (_study != null)
                {
                    ClearStudyInfo();
                    ClearPatientInfo();
                    return UpdateClientModel();
                }
                else
                {
                    internalModel.StudyOpenErrorDiv = "<div><p>No study ID given.</p></div>";
                }
            }

            return View(VIEW_NAME, internalModel);
        }

        [Route("saveSettings")]
        [HttpPost]
        public IActionResult SaveSettings(string fhirServer)
        {
            //return PartialView("ErrorModal");
            if (fhirServer != internalModel.FHIRServer)

            {
                internalModel.FHIRServer = fhirServer;
                createFHIRClient(internalModel.FHIRServer);
            }
            
            return View(VIEW_NAME, internalModel);
        }

        private IActionResult UpdateClientModel()
        {
            //Study is nothing just use patient
            //Otherwise get patient from study
            if (_study != null)
            {
                _patient = GetPatient("", _study.Patient.Reference);

                internalModel.AccessionNumber = (_study.Accession != null) ? _study.Accession.Value : "";
                internalModel.StudyId = _study.Uid;
            }
            else
            {
                ClearStudyInfo();
            }

            if (_patient == null)
            {
                ClearPatientInfo();
            }
            else
            {
                internalModel.PatientName = $"{_patient.Name[0].Family}, {_patient.Name[0].Given.FirstOrDefault()}";
                internalModel.PatientDOB = _patient.BirthDate;
            }


            internalModel.Patient = _patient;
            return View(VIEW_NAME, internalModel);
        }

        private void ClearPatientInfo()
        {
            internalModel.PatientName = "";
            internalModel.PatientDOB = "";
            internalModel.PatientOpenErrorDiv = "";
        }

        private void ClearStudyInfo()
        {
            internalModel.StudyId = "";
            internalModel.AccessionNumber = "";
            internalModel.StudyOpenErrorDiv = "";
        }

        private Patient GetPatient(string id, string patientURI = "")
        {
            Uri uri = new Uri(client.Endpoint + "Patient/" + id);

            try
            {
                if (patientURI.Length > 0) { return client.Read<Patient>(patientURI); }
                else { return client.Read<Patient>(uri); }
            }
            catch (FhirOperationException ex)
            {
                foreach (var item in ex.Outcome.Children)
                {
                    Narrative nar = item as Narrative;
                    if (nar != null)
                    {
                        internalModel.PatientOpenErrorDiv = nar.Div;
                    }
                }
                return null;
            }
        }

        private ImagingStudy GetStudy(string id)
        {
            Uri uri = new Uri(client.Endpoint + "ImagingStudy/" + id);
            try
            {
                return client.Read<ImagingStudy>(uri);
            }
            catch (FhirOperationException ex)
            {
                foreach (var item in ex.Outcome.Children)
                {
                    Narrative nar = item as Narrative;
                    if (nar != null)
                    {
                        internalModel.StudyOpenErrorDiv = nar.Div;
                    }
                }
                return null;
            }
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
        public IActionResult Get(string subscriptionId, [FromQuery] SubscriptionVerification verification) {
            //Received a verification request for non-pending subscription, return a NotFound response
            if (!pendingSubs.ContainsKey(subscriptionId)) { return NotFound(); }

            Model.Subscription sub = pendingSubs[subscriptionId];

            //Validate verification subcription with our subscription. If a match return challenge
            //otherwise return NotFound response.
            if (SubsEqual(sub, verification)) {
                //Move subscription to active sub collection and remove from pending subs
                activeSubs.Add(subscriptionId, sub);
                pendingSubs.Remove(subscriptionId);
                return this.Content(verification.Challenge);
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

            var rngCsp = new RNGCryptoServiceProvider();
            var buffer = new byte[100];
            rngCsp.GetBytes(buffer);
            var secret = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var httpClient = new HttpClient();
            string subUID = Guid.NewGuid().ToString("n");
            var data = new Model.Subscription()
            {
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

            this.logger.LogDebug($"Subscription for 'subscribing': {Environment.NewLine}{data}");

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
            return View(VIEW_NAME, internalModel);
        }

        [Route("unsubscribe/{subscriptionId}")]
        [HttpPost]
        public async Task<IActionResult> Unsubscribe(string subscriptionId) {
            this.logger.LogDebug($"Unsubscribing subscription {subscriptionId}");
            if (!activeSubs.ContainsKey(subscriptionId)) { return View(VIEW_NAME, internalModel); }
            Model.Subscription sub = activeSubs[subscriptionId];
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

            return View(VIEW_NAME, internalModel);
        }
        #endregion

        #region Private methods
        private bool SubsEqual(SubscriptionBase sub1, SubscriptionBase sub2) {
            return sub1.Callback == sub2.Callback && sub1.Topic == sub2.Topic;
        }
        #endregion
    }
}
