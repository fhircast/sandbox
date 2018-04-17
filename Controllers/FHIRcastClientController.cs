using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NLog.Targets;
using FHIRcastSandbox.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class FHIRcastClientController : Controller {
        [HttpGet]
        public IActionResult Get() {
            return View("View", new ClientModel { PatientIdentifier = ""});
        }

        [HttpPost]
        public IActionResult Post([FromForm] ClientModel model) {

            RedirectToAction("Hub", "Notify", model);

            return View("View", model);
        }

    }
}
