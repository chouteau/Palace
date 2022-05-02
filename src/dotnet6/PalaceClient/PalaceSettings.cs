using System;
using System.Collections.Generic;
using System.Text;

namespace PalaceClient
{
    public class PalaceSettings
    {
        public PalaceSettings()
        {
            StartedDate = DateTime.Now;
            TimeoutInSecondBeforeKillService = 30;
        }
        public string ApiKey { get; set; }
        public string ServiceName { get; set; }
        public string Version { get; internal set; }
        public string PalaceClientVersion { get; internal set; }
        public string Location { get; internal set; }
        public DateTime LastWriteTime { get; internal set; }
        public DateTime StartedDate { get; set; }
        public string HostEnvironmentName { get; set; }
		public int TimeoutInSecondBeforeKillService { get; set; }
	}
}
