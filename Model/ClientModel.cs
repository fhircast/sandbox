using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FHIRcastSandbox.Model {
    public class ClientModel : ModelBase {
        public ClientModel()
        {
            ActiveSubscriptions = new List<Subscription>();
<<<<<<< HEAD
            SubscriptionsToHub = new List<Subscription>();
=======
>>>>>>> eb6a56c3119cc39dc48f9759eb57b6554107270d
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
<<<<<<< HEAD
        public List<Subscription> SubscriptionsToHub { get; set; }
=======
>>>>>>> eb6a56c3119cc39dc48f9759eb57b6554107270d
    }
}
