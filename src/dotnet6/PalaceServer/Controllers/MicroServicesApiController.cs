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
        [Route("download/{packageFileName}")]
        public IActionResult DownloadPackage([FromHeader] string authorization, string packageFileName)
        {
            if (string.IsNullOrWhiteSpace(packageFileName))
            {
                return BadRequest();
            }

            EnsureGoodAuthorization(authorization);

            var list = Collector.GetAvailablePackageList();
            var serviceInfo = list.FirstOrDefault(i => i.PackageFileName.Equals(packageFileName, StringComparison.InvariantCultureIgnoreCase));
            if (serviceInfo == null)
            {
                return NotFound($"this service name {packageFileName} does not exist");
            }

            var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());

            if (serviceInfo.LockedBy != null
                && serviceInfo.LockedBy != palaceInfo.Key)
            {
                return NoContent();
            }

            var fileName = System.IO.Path.Combine(PalaceServerSettings.MicroServiceRepositoryFolder, packageFileName);

            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/zip", packageFileName);
        }

        [HttpGet]
        [Route("info/{packageFileName}")]
        public IActionResult GetMicroServicesInfo([FromHeader] string authorization, string packageFileName)
        {
            if (string.IsNullOrWhiteSpace(packageFileName))
            {
                return BadRequest($"Bad packageName {packageFileName}");
            }

            EnsureGoodAuthorization(authorization);

            var list = Collector.GetAvailablePackageList();
            var serviceInfo = list.FirstOrDefault(i => i.PackageFileName.Equals(packageFileName, StringComparison.InvariantCultureIgnoreCase));
            return Ok(serviceInfo);
        }

        [HttpGet]
        [Route("list")]
        public IActionResult GetMicroServicesList([FromHeader] string authorization)
        {
            EnsureGoodAuthorization(authorization);

            var list = Collector.GetAvailablePackageList();
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
            EnsureGoodAuthorization(authorization);

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

        [HttpGet]
        [Route("configuration")]
        public IActionResult GetConfiguration([FromHeader] string authorization)
        {
            EnsureGoodAuthorization(authorization);

            var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
            if (palaceInfo == null)
            {
                return NoContent();
            }

            return Ok(palaceInfo.MicroServiceSettingsList);
        }

        [HttpPost]
        [Route("addservice")]
        public IActionResult AddService([FromHeader] string authorization, Models.MicroServiceSettings serviceSettings)
		{
            EnsureGoodAuthorization(authorization);

            var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
            if (palaceInfo == null)
            {
                return NoContent();
            }

            var result = PalaceInfoManager.SaveMicroServiceSettings(palaceInfo,serviceSettings);
            if (result.Count > 0)
			{
                BadRequest(result);
            }
            return Ok();
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
