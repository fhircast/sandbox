using Newtonsoft.Json;

namespace FHIRcastSandbox.Model {
    public abstract class ModelBase {
        public override string ToString() {
            return JsonConvert.SerializeObject(this);
        }
    }
}
