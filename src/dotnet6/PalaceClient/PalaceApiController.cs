using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PalaceClient
{
    [ApiController]
    [Route("api/palace")]
    public class PalaceApiController : ControllerBase
    {
        private readonly PalaceSettings _palaceSettings;
        private readonly System.Timers.Timer _timer;
        private readonly ILogger _logger;

        public PalaceApiController(PalaceSettings palaceSettings,
            ILogger<PalaceApiController> logger)
        {
            this._palaceSettings = palaceSettings;
            this._logger = logger;
            this._timer = new System.Timers.Timer();
            this._timer.Interval = _palaceSettings.TimeoutInSecondBeforeKillService * 1000;
			this._timer.Elapsed += ExitTimerElapsed;
        }

		[HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                DateTime = DateTime.Now,
            });
        }

        [HttpGet]
        [Route("infos")]
        public IActionResult GetServiceInfo([FromHeader] string authorization)
        {
            EnsureGoodAuthorization(authorization);

            return Ok(new RunningMicroserviceInfo
            {
                ServiceName = _palaceSettings.ServiceName,
                Version = _palaceSettings.Version,
                Location = _palaceSettings.Location,
                UserInteractive = System.Environment.UserInteractive,
                LastWriteTime = _palaceSettings.LastWriteTime,
                ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                StartedDate = _palaceSettings.StartedDate,
                CommandLine = System.Environment.CommandLine,
                EnvironmentName = _palaceSettings.HostEnvironmentName,
                AdminUrl = $"{Request.Scheme}://{Request.Host}",
                PalaceClientVersion = _palaceSettings.PalaceClientVersion,
            });
        }

        [HttpGet]
        [Route("stop")]
        public IActionResult Stop([FromHeader] string authorization)
        {
            EnsureGoodAuthorization(authorization);

			_logger.LogInformation($"Try to close the service {_palaceSettings.ServiceName}");
			var mre = AppDomain.CurrentDomain.GetData(StopAwaiter.PALACE_STOPPER_EVENT) as ManualResetEvent;
            if (mre == null)
            {
                _logger.LogError($"try to get reset from AppDomain fail for service {_palaceSettings.ServiceName}");
                return Ok(new PalaceClient.StopResult
                {
                    Message = "fail"
                });
            }
            _timer.Start();
            mre.Set();
            return Ok(new PalaceClient.StopResult
            {
                Message = "stopping"
            });
        }

        private void EnsureGoodAuthorization(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
            {
                throw new UnauthorizedAccessException("api key needed");
            }
            if (authorization.IndexOf(_palaceSettings.ApiKey, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                throw new UnauthorizedAccessException("bad api key");
            }
        }

        private void ExitTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _logger.LogWarning($"Try to close the service {_palaceSettings.ServiceName} fail with soft method");
            _timer.Stop();
            Environment.Exit(0);
        }
    }
}
