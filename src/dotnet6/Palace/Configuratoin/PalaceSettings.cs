using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Configuration
{
	public class PalaceSettings
	{
		public PalaceSettings()
		{
			ScanIntervalInSeconds = 15;
			BackupDirectory = @".\backup";
			UpdateDirectory = @".\update";
			DownloadDirectory = @".\download";
			InstallationDirectory = @".\microservices";
			WaitingUpdateTimeoutInSecond = 30;
			StopAllMicroServicesWhenStop = false;
		}

        public string UpdateServerUrl { get; set; }
        public string BackupDirectory { get; set; }
        public string UpdateDirectory { get; set; }
        public string DownloadDirectory { get; set; }
        public string InstallationDirectory { get; set; }

        public string HostName { get; set; }

		public int ScanIntervalInSeconds { get; set; }
		public string ApiKey { get; set; }

        public int WaitingUpdateTimeoutInSecond { get; set; }
        public bool StopAllMicroServicesWhenStop { get; set; }
        public LogLevel LogLevel { get; set; }

		[Obsolete("Use palaceServer instead", true)]
        public string PalaceServicesFileName { get; set; }
	}
}
