using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using PalaceServer.Extensions;

namespace PalaceServer.Controllers
{
    [ApiController]
    [Route("api/microservices")]
    public class MicroServicesApiController : ControllerBase
    {
        public MicroServicesApiController(Configuration.PalaceServerSettings palaceServicerSettings,
            Services.MicroServiceCollectorManager microServicesService,
            Services.PalaceInfoManager palaceInfoManager)
        {
            this.PalaceServerSettings = palaceServicerSettings;
            this.Collector = microServicesService;
            this.PalaceInfoManager = palaceInfoManager;
        }

        protected Configuration.PalaceServerSettings PalaceServerSettings { get; }
        protected Services.MicroServiceCollectorManager Collector { get; }
        protected Services.PalaceInfoManager PalaceInfoManager { get; }

        [Route("ping")]
        [HttpGet]
        public IActionResult Ping()
        {
            return Ok(new
            {
                DateTime = DateTime.Now,
            });
        }

        [HttpGet]
        [Route("download/{serviceName}")]
        public IActionResult DownloadMicroService([FromHeader] string authorization, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return BadRequest($"Bad serviceName {serviceName}");
            }

            EnsureGoodAuthorization(authorization);

            var list = Collector.GetAvailableList();
            var serviceInfo = list.FirstOrDefault(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
            if (serviceName == null)
            {
                return NotFound($"this service name {serviceName} does not exist");
            }

            var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
            if (serviceInfo.LockedBy != null
                && serviceInfo.LockedBy != palaceInfo.Key)
            {
                return NoContent();
            }

            var fileName = System.IO.Path.Combine(PalaceServerSettings.MicroServiceRepositoryFolder, serviceInfo.ZipFileName);

            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            return File(stream, "application/zip", serviceInfo.ZipFileName);
        }

        [HttpGet]
        [Route("info/{serviceName}")]
        public IActionResult GetMicroServicesInfo([FromHeader] string authorization, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return BadRequest($"Bad serviceName {serviceName}");
            }

            EnsureGoodAuthorization(authorization);

            var list = Collector.GetAvailableList();
            var serviceInfo = list.FirstOrDefault(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
            return Ok(serviceInfo);
        }

        [HttpGet]
        [Route("list")]
        public IActionResult GetMicroServicesList([FromHeader] string authorization)
        {
            EnsureGoodAuthorization(authorization);

            var list = Collector.GetAvailableList();
            return Ok(list);
        }

        [HttpPost]
        [Route("registerorupdateinfos")]
        public IActionResult RegisterOrUpdateRunningMicroServiceInfo([FromHeader] string authorization, PalaceClient.RunningMicroserviceInfo runningMicroserviceInfo)
        {
            EnsureGoodAuthorization(authorization);

            Collector.AddOrUpdateRunningMicroServiceInfo(runningMicroserviceInfo, HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
            return Ok();
        }

        [HttpPost]
        [Route("updateserviceproperties")]
        public IActionResult UpdateRunningMicroServiceProperties([FromHeader] string authorization, Models.ServiceProperties serviceProperties)
        {
            EnsureGoodAuthorization(authorization);

            Collector.UpdateRunningMicroServiceProperties(serviceProperties, HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
            return Ok();
        }

        [HttpGet]
        [Route("getnextaction/{serviceName}")]
        public IActionResult GetAction([FromHeader] string authorization, string serviceName)
        {
            var svc = Collector.GetRunningList().FirstOrDefault(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
            if (svc == null)
            {
                return Ok(new Models.NextActionResult
                {
                    Action = Models.ServiceAction.DoNothing
                });
            }

            var nextAction = svc.NextAction;
            svc.NextAction = Models.ServiceAction.DoNothing;

            return Ok(new Models.NextActionResult
            {
                Action = nextAction
            });
        }

        private void EnsureGoodAuthorization(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
            {
                throw new UnauthorizedAccessException("api key needed");
            }
            if (authorization.IndexOf(PalaceServerSettings.ApiKey, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                throw new UnauthorizedAccessException("bad api key");
            }
        }
    }
}
