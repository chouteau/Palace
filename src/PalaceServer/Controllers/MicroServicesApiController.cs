using System.Reflection.PortableExecutable;

using Microsoft.AspNetCore.Mvc;

namespace PalaceServer.Controllers;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/microservices")]
public class MicroServicesApiController : ControllerBase
{
    public MicroServicesApiController(Configuration.PalaceServerSettings palaceServicerSettings,
        Services.MicroServiceCollectorManager microServicesService,
        Services.PalaceInfoManager palaceInfoManager,
        ILogger<MicroServicesApiController> logger)
    {
        this.PalaceServerSettings = palaceServicerSettings;
        this.Collector = microServicesService;
        this.PalaceInfoManager = palaceInfoManager;
        this.Logger = logger;
    }

    protected Configuration.PalaceServerSettings PalaceServerSettings { get; }
    protected Services.MicroServiceCollectorManager Collector { get; }
    protected Services.PalaceInfoManager PalaceInfoManager { get; }
    protected ILogger Logger { get; }

    [Microsoft.AspNetCore.Mvc.Route("ping")]
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new
        {
            DateTime = DateTime.Now,
        });
    }

    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("download/{packageFileName}")]
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

    [HttpPost]
    [Microsoft.AspNetCore.Mvc.Route("upload-package")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadPackage([FromHeader] string authorization)
    {
        EnsureGoodAuthorization(authorization);

        if (!Request.HasFormContentType)
        {
            var message = "accept only mimetype 'multipart/form-data'";
            Logger.LogWarning(message);
            return BadRequest(message);
        }

        var form = await Request.ReadFormAsync();

        if (!form.Files.Any())
        {
            var message = "there is no package";
            Logger.LogWarning(message);
            return BadRequest(message);
        }

        var package = form.Files.First();
        if (package is null || package.Length == 0)
        {
            var message = "package is null or empty";
            Logger.LogWarning(message);
            return BadRequest(message);
        }

        Logger.LogInformation($"{package.FileName} uploaded");
        var fileName = System.IO.Path.GetFileName(package.FileName);
        var destination = System.IO.Path.Combine(PalaceServerSettings.MicroServiceStagingFolder, fileName);
        if (System.IO.File.Exists(destination))
        {
            System.IO.File.Delete(destination);
        }
        using var writer = System.IO.File.Create(destination);
        await package.CopyToAsync(writer);

        Logger.LogInformation($"{package.FileName} deployed to {destination}");

        return Ok();
    }

    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("info/{packageFileName}")]
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
    [Microsoft.AspNetCore.Mvc.Route("list")]
    public IActionResult GetMicroServicesList([FromHeader] string authorization)
    {
        EnsureGoodAuthorization(authorization);

        var list = Collector.GetAvailablePackageList();
        return Ok(list);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Mvc.Route("registerorupdateinfos")]
    public IActionResult RegisterOrUpdateRunningMicroServiceInfo([FromHeader] string authorization, PalaceClient.RunningMicroserviceInfo runningMicroserviceInfo)
    {
        EnsureGoodAuthorization(authorization);

        Collector.AddOrUpdateRunningMicroServiceInfo(runningMicroserviceInfo, HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
        return Ok();
    }

    [HttpPost]
    [Microsoft.AspNetCore.Mvc.Route("updateserviceproperties")]
    public IActionResult UpdateRunningMicroServiceProperties([FromHeader] string authorization, Models.ServiceProperties serviceProperties)
    {
        EnsureGoodAuthorization(authorization);

        Collector.UpdateRunningMicroServiceProperties(serviceProperties, HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
        return Ok();
    }

    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("getnextaction/{serviceName}")]
    public IActionResult GetAction([FromHeader] string authorization, string serviceName)
    {
        EnsureGoodAuthorization(authorization);

        var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(HttpContext.GetUserAgent(), HttpContext.GetUserHostAddress());
        var key = $"{palaceInfo.MachineName}.{palaceInfo.HostName}.{serviceName}".ToLower();
        var svc = Collector.GetRunningList().FirstOrDefault(i => i.Key == key);
        if (svc == null)
        {
            return Ok(new Models.NextActionResult
            {
                Action = Models.ServiceAction.DoNothing
            });
        }
        var nextAction = svc.NextAction;
        if (nextAction != Models.ServiceAction.DoNothing)
			{
            Logger.LogInformation($"Action {nextAction} required for running service {serviceName} on {svc.Key}");
			}
        var result = new Models.NextActionResult
        {
            Action = nextAction
        };

        svc.NextAction = Models.ServiceAction.DoNothing;
        return Ok(result);
    }

    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("configuration")]
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
    [Microsoft.AspNetCore.Mvc.Route("addorupdateservicesettings")]
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
