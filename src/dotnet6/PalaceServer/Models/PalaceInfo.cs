using System.Linq.Expressions;

namespace PalaceServer.Models
{
    public class PalaceInfo
    {
        public PalaceInfo(string userAgent, string userHostAddress)
        {
			ParseUserAgent(userAgent);
			Ip = userHostAddress;
			LastHitDate = DateTime.Now;
			MicroServiceSettingsList = new List<MicroServiceSettings>();
		}

        public string Os { get; set; }
        public string MachineName { get; set; }
        public string HostName { get; set; }
        public string Version { get; set; }
        public string Ip { get; set; }
        public DateTime LastHitDate { get; set; }

        public IEnumerable<MicroServiceSettings> MicroServiceSettingsList { get; set; }
        public DateTime? LastConfigurationUpdate { get; set; }

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

		public Dictionary<string, List<string>> AddMicroServiceSettings(Models.MicroServiceSettings settings)
        {
            var existing = MicroServiceSettingsList.FirstOrDefault(i => i.ServiceName == settings.ServiceName);
            if (existing != null)
            {
                // Pas normal
                var result = new Dictionary<string, List<string>>();
                result.Add(
                    nameof(existing.ServiceName),
                    new List<string>
                    {
                        { "Service already exists" }
                    });
                return result;
            }

            var validation = Validate(settings);
            if (!validation.IsValid)
            {
                return validation.BrokenRules;
            }
            ((List<MicroServiceSettings>)MicroServiceSettingsList).Add(settings);
            LastConfigurationUpdate = DateTime.Now;
            return new Dictionary<string, List<string>>();
        }

        internal Dictionary<string, List<string>> UpdateMicroServiceSettings(Models.MicroServiceSettings settings)
        {
            var validation = Validate(settings);
            if (!validation.IsValid)
            {
                return validation.BrokenRules;
            }
            var existing = MicroServiceSettingsList.FirstOrDefault(i => i.ServiceName == settings.ServiceName);

            bool isDirty = false;
            if (existing.AdminServiceUrl != settings.AdminServiceUrl)
            {
                isDirty = true;
            }
            existing.AdminServiceUrl = settings.AdminServiceUrl;
            if (existing.AlwaysStarted != settings.AlwaysStarted)
            {
                isDirty = true;
            }
            existing.AlwaysStarted = settings.AlwaysStarted;
            if (existing.Arguments != settings.Arguments)
            {
                isDirty = true;
            }
            existing.Arguments = settings.Arguments;
            if (existing.InstallationFolder != settings.InstallationFolder)
            {
                isDirty = true;
            }
            existing.InstallationFolder = settings.InstallationFolder;
            if (existing.MainAssembly != settings.MainAssembly)
            {
                isDirty = true;
            }
            existing.MainAssembly = settings.MainAssembly;
            if (existing.PackageFileName != settings.PackageFileName)
            {
                isDirty = true;
            }
            existing.PackageFileName = settings.PackageFileName;
            if (existing.PalaceApiKey != settings.PalaceApiKey)
            {
                isDirty = true;
            }
            existing.PalaceApiKey = settings.PalaceApiKey;
            if (existing.ServiceName != settings.ServiceName)
            {
                isDirty = true;
            }
            existing.ServiceName = settings.ServiceName;
            if (existing.SSLCertificate != settings.SSLCertificate)
            {
                isDirty = true;
            }
            existing.SSLCertificate = settings.SSLCertificate;

            if (isDirty)
            {
                LastConfigurationUpdate = DateTime.Now;
            }
            return new Dictionary<string, List<string>>();
        }

        internal (bool IsValid, Dictionary<string,List<string>> BrokenRules) Validate(Models.MicroServiceSettings mss)
        {
            var result = true;
            var brokenRules = new Dictionary<string, List<string>>();
            if (string.IsNullOrWhiteSpace(mss.ServiceName))
            {
                brokenRules.Add("ServiceName", new List<string>
                {
                    { "Service name is null or empty" }
                });
                result = false;
            }

            if (string.IsNullOrWhiteSpace(mss.MainAssembly))
            {
                brokenRules.Add(nameof(mss.MainAssembly), new List<string>
                {
                    { "MainAssembly is null or empty" }
                });
                result = false;
            }
            if (string.IsNullOrWhiteSpace(mss.PackageFileName))
            {
                brokenRules.Add(nameof(mss.PackageFileName), new List<string>
                {
                    { "PackageFileName is null or empty" }
                });
                result = false;
            }
            else if (!mss.PackageFileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                brokenRules.Add(nameof(mss.PackageFileName), new List<string>
                {
                    { "PackageFileName is not zip file" }
                });
                result = false;
            }
            if (string.IsNullOrWhiteSpace(mss.AdminServiceUrl))
            {
                brokenRules.Add(nameof(mss.AdminServiceUrl), new List<string>
                {
                    { "AdminServiceUrl is null or empty" }
                });
                result = false;
            }
            else
            {
                try
                {
                    new Uri(mss.AdminServiceUrl);
                }
                catch
                {
                    brokenRules.Add(nameof(mss.AdminServiceUrl), new List<string>
                    {
                        { "AdminServiceUrl is not valid uri" }
                    });
                    result = false;
                }
            }


            return (result, brokenRules);
        }

        internal void RemoveMicroServiceSettings(string serviceName)
        {
            var existing = MicroServiceSettingsList.FirstOrDefault(i => i.ServiceName == serviceName);
            if (existing != null)
            {
                ((List<MicroServiceSettings>)MicroServiceSettingsList).Remove(existing);
                LastConfigurationUpdate = DateTime.Now;
            }
        }
    }
}
