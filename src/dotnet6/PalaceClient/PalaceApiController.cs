using Microsoft.AspNetCore.Mvc;
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

        public PalaceApiController(PalaceSettings palaceSettings)
        {
            this._palaceSettings = palaceSettings;  
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
                CommandLine = System.Environment.CommandLine
            });
        }

        [HttpGet]
        [Route("stop")]
        public IActionResult Stop([FromHeader] string authorization)
        {
            EnsureGoodAuthorization(authorization);

            var mre = AppDomain.CurrentDomain.GetData("StopperEvent") as ManualResetEvent;
            if (mre == null)
            {
                return Ok(new PalaceClient.StopResult
                {
                    Message = "fail"
                });
            }
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

    }
}
