using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	public class Settings
	{
		public Settings()
		{
			ScanIntervalInSeconds = "60";
			ServiceName = "Palace";
			ServiceDisplayName = "Palace Services Hoster";
			ServiceDescription = "Host for (auto updatable) services";
			UpdateUriList = new List<string>();
		}

		public string ServiceName { get; set; }
		public string ServiceDisplayName { get; set; }
		public string ServiceDescription { get; set; }

		public List<string> UpdateUriList { get; set; }
		public string ScanIntervalInSeconds { get; set; }
		public string ApiKey { get; set; }
	}
}
