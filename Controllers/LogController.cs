using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NLog.Targets;

namespace FHIRcastSandbox.Controllers {
    [Route("api/[controller]")]
    public class LogController : Controller {
        [HttpGet]
        [Route("{log}")]
        public IActionResult Get(string log) {
            var target = LogManager.Configuration.FindTargetByName<FileTarget>(log);
            if (target != null) {
                var logFile = target.FileName.Render(new LogEventInfo());
                var logDir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);

                return this.Content(System.IO.File.ReadAllText(Path.Combine(logDir, logFile)));
            } else {
                return this.NotFound("No such log file found...");
            }
        }

        [HttpGet]
        public IActionResult Get() {
            return this.Get("fhircast");
        }
    }
}
