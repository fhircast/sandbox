using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Hl7.Fhir.Model;

namespace FHIRcastSandbox.Model {
    public class ClientModel : ModelBase {
        public ClientModel()
        {
            ActiveSubscriptions = new List<Subscription>();
            SubscriptionsToHub = new List<Subscription>();
            PatientSearchOptions = new Dictionary<string, string>();
            SearchPatients = new SelectList(new List<SelectListItem>());
        }
        public string UserIdentifier { get; set; }
        public string PatientIdentifier { get; set; }
        public string PatientIdIssuer { get; set; }
        
        public string AccessionNumberGroup { get; set; }
        
        public string Event { get; set; }
        public string Topic { get; set; }
        public List<Subscription> ActiveSubscriptions { get; set; }
        public List<Subscription> SubscriptionsToHub { get; set; }

        //Patient Info
        public Patient Patient { get; set; }
        public string PatientName { get; set; }
        public string PatientDOB { get; set; }
        public string PatientOpenErrorDiv { get; set; }

        public string SelectedPatientID { get; set; }
        public IEnumerable<SelectListItem> SearchPatients { get; set; }
        public Dictionary<string, string> PatientSearchOptions { get; set; }

        //Study Info
        public string StudyId { get; set; }
        public string AccessionNumber { get; set; }
        public string StudyOpenErrorDiv { get; set; }

        //FHIR Server Info
        public string FHIRServer { get; set; }
    }
}
