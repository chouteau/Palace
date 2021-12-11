namespace PalaceServer.Models
{
    public class PalaceInfo
    {
        public PalaceInfo(string userAgent, string userHostAddress)
        {
			ParseUserAgent(userAgent);
			Ip = userHostAddress;
        }
        public string Os { get; set; }
        public string MachineName { get; set; }
        public string HostName { get; set; }
        public string Version { get; set; }
        public string Ip { get; set; }

		public string Key
        {
            get
            {
				return $"{MachineName}.{HostName}";
            }
        }

        internal void ParseUserAgent(string userAgent)
		{
			if (string.IsNullOrWhiteSpace(userAgent))
			{
				return;
			}

			var pattern = @"Palace/(?<version>[^\(]*)\((?<os>[^;]*);(?<machineName>[^;]*);(?<hostName>[^;]*)\)";
			var regexp = new System.Text.RegularExpressions.Regex(pattern);
			var match = regexp.Match(userAgent);

			if (!match.Success)
			{
				return;
			}

			this.Os = match.Groups["os"].Value.Trim();
			this.MachineName = match.Groups["machineName"].Value.Trim();
			this.HostName = match.Groups["hostName"].Value.Trim();
			this.Version = match.Groups["version"].Value.Trim();
		}

	}
}
