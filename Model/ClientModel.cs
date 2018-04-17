﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FHIRcastSandbox.Model {
    public class ClientModel : ModelBase {
        public string UserIdentifier { get; set; }
        public string PatientIdentifier { get; set; }
        public string PatientIdIssuer { get; set; }
        public string AccessionNumber { get; set; }
        public string AccessionNumberGroup { get; set; }
        public string StudyId { get; set; }
        public string Event { get; set; }
        public string Topic { get; set; }
    }
}