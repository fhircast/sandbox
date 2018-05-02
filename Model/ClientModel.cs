using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FHIRcastSandbox.Model {
    public class ClientModel : ModelBase {
        public ClientModel()
        {
            ActiveSubscriptions = new List<Subscription>();
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
    }
}
