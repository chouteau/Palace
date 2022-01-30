using System.Linq.Expressions;

namespace PalaceServer.Models
{
    public class PalaceInfo
    {
        public PalaceInfo(string userAgent, string userHostAddress, Configuration.PalaceServerSettings palaceServerSettings)
        {
			ParseUserAgent(userAgent);
			Ip = userHostAddress;
			LastHitDate = DateTime.Now;
            this.Settings = palaceServerSettings;
		}

        protected Configuration.PalaceServerSettings Settings { get; }

        public string Os { get; set; }
        public string MachineName { get; set; }
        public string HostName { get; set; }
        public string Version { get; set; }
        public string Ip { get; set; }
        public DateTime LastHitDate { get; set; }

        private IEnumerable<MicroServiceSettings> _microServiceSettingsList;
        public IEnumerable<MicroServiceSettings> MicroServiceSettingsList 
        { 
            get
            {
                if (_microServiceSettingsList == null)
                {
                    var configFileName = System.IO.Path.Combine(Settings.MicroServiceConfigurationFolder, $"{Key}.json");
                    if (System.IO.File.Exists(configFileName))
                    {
                        var content = System.IO.File.ReadAllText(configFileName);
                        var list = System.Text.Json.JsonSerializer.Deserialize<List<MicroServiceSettings>>(content);
                        _microServiceSettingsList = list;
                    }
                    else
                    {
                        _microServiceSettingsList = new List<MicroServiceSettings>();
                    }
                }
                return _microServiceSettingsList;
            }
        }
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
            SaveConfiguration();
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

            isDirty = existing.SSLCertificate != settings.SSLCertificate;
            existing.SSLCertificate = settings.SSLCertificate;

            isDirty = existing.InstanceCount != settings.InstanceCount;
            existing.InstanceCount = settings.InstanceCount;

            if (isDirty)
            {
                LastConfigurationUpdate = DateTime.Now;
                SaveConfiguration();
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
                SaveConfiguration();
            }
        }

        private void SaveConfiguration()
        {
            var configFileName = System.IO.Path.Combine(Settings.MicroServiceConfigurationFolder, $"{Key}.json");
            var content = System.Text.Json.JsonSerializer.Serialize(MicroServiceSettingsList, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
            });
            if (System.IO.File.Exists(configFileName))
            {
                System.IO.File.Copy(configFileName, $"{configFileName}.bak", true);
            }
            System.IO.File.WriteAllText(configFileName, content);
        }
    }
}
