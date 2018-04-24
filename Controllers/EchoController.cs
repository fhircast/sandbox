using FHIRcastSandbox.Model;
using Microsoft.AspNetCore.Mvc;

namespace FHIRcastSandbox.Controllers
{
    [Route("api/[controller]")]
    public class EchoController : Controller {
        [HttpGet]
        public IActionResult Get([FromQuery] SubscriptionVerification verification) {
            return this.Content(verification.Challenge);
        }

        [HttpPost]
        public IActionResult Post([FromBody] Notification notification) {
            FHIRcastClientController.pubModel = new ClientModel()
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
    }
}
