﻿using System;
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
			ScanIntervalInSeconds = 60;
			MicroServiceInfoList = new List<MicroServiceSettings>();
			BackupDirectory = @".\backup";
			UpdateDirectory = @".\update";
			DownloadDirectory = @".\download";
			WaitingUpdateTimeoutInSecond = 30;
			StopAllMicroServicesWhenStop = false;
		}

        public string UpdateServerUrl { get; set; }
        public string BackupDirectory { get; set; }
        public string UpdateDirectory { get; set; }
        public string DownloadDirectory { get; set; }

        public string HostName { get; set; }

        public List<MicroServiceSettings> MicroServiceInfoList { get; set; }
		public int ScanIntervalInSeconds { get; set; }
		public string ApiKey { get; set; }

        public int WaitingUpdateTimeoutInSecond { get; set; }
        public bool StopAllMicroServicesWhenStop { get; set; }

        public void Initialize()
        {
			var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
			if (BackupDirectory.StartsWith(@".\"))
            {
				BackupDirectory = System.IO.Path.Combine(currentDirectory, BackupDirectory.Replace(@".\", string.Empty));
            }
			if (UpdateDirectory.StartsWith(@".\"))
			{
				UpdateDirectory = System.IO.Path.Combine(currentDirectory, UpdateDirectory.Replace(@".\", string.Empty));
			}
			if (DownloadDirectory.StartsWith(@".\"))
			{
				DownloadDirectory = System.IO.Path.Combine(currentDirectory, DownloadDirectory.Replace(@".\", string.Empty));
			}
		}
	}
}