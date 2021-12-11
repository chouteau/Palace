using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using PalaceServer.Extensions;


namespace PalaceServer.Controllers
{
    [ApiController]
    [Route("api/logging")]

    public class LoggerApiController : ControllerBase
    {
        public LoggerApiController(Services.LogCollector logCollector)
        {
            this.LogCollector = logCollector;
        }

        protected Services.LogCollector LogCollector { get; }

        [HttpPost]
        [Route("writelog")]
        public async Task<IActionResult> WriteLog(Models.LogInfo logInfo)
        {
            LogCollector.AddLog(logInfo);
            return Ok();
        }
    }
}
