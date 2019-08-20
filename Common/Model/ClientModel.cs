using System.Collections.Generic;

namespace FHIRcastSandbox.Model
{
    public class ClientModel : ModelBase {
        public string PatientID { get; set; }
        public string AccessionNumber { get; set; }
    }
}
