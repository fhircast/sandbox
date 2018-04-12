using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class HubController : Controller {
        private readonly ILogger<HubController> logger;

        public HubController(ILogger<HubController> logger) {
            this.logger = logger;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value) {
        }
    }
}
