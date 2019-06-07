using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FHIRcastSandbox.Model {
    public class ClientModel : ModelBase {
        public ClientModel()
        {
            ActiveSubscriptions = new List<Subscription>();
            SubscriptionsToHub = new List<Subscription>();
        }
        public string UserIdentifier { get; set; }
        public string PatientIdentifier { get; set; }
        public string PatientIdIssuer { get; set; }
        public string AccessionNumber { get; set; }
        public string AccessionNumberGroup { get; set; }
        public string StudyId { get; set; }
        public string Event { get; set; }
        public string Topic { get; set; }
        public List<Subscription> ActiveSubscriptions { get; set; }
        public List<Subscription> SubscriptionsToHub { get; set; }

        public ClientModel(JObject jObject)
        {
            foreach (KeyValuePair<string, JToken> kvp in jObject)
            {
                switch (kvp.Key)
                {
                    case "userIdentifier":
                        this.UserIdentifier = kvp.Value.ToString();
                        break;
                    case "accessionNumber":
                        this.AccessionNumber = kvp.Value.ToString();
                        break;
                    case "patientIdentifier":
                        this.PatientIdentifier = kvp.Value.ToString();
                        break;
                    case "accessionNumberGroup":
                        this.AccessionNumberGroup = kvp.Value.ToString();
                        break;
                    case "patientIdIssuer":
                        this.PatientIdIssuer = kvp.Value.ToString();
                        break;
                    case "studyId":
                        this.StudyId = kvp.Value.ToString();
                        break;
                    case "topic":
                        this.Topic = kvp.Value.ToString();
                        break;
                    case "event":
                        this.Event = kvp.Value.ToString();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
