namespace PalaceServer.Services;

public class PalaceInfoManager
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo> _palaceInfoDictionary;

    public PalaceInfoManager(ILogger<PalaceInfoManager> logger,
        Configuration.PalaceServerSettings palaceServerSettings)
    {
        this.Logger = logger;   
        this.PalaceServerSettings = palaceServerSettings;   
        _palaceInfoDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.PalaceInfo>();
    }

    protected ILogger Logger { get; }
    protected Configuration.PalaceServerSettings PalaceServerSettings { get; }

    public IEnumerable<Models.PalaceInfo> GetPalaceInfoList()
    {
        return _palaceInfoDictionary.Values;
    }

    public Models.PalaceInfo GetOrCreatePalaceInfo(string userAgent, string userHostAddress)
    {
        var pi = new Models.PalaceInfo();
        FillWithUserAgent(pi, userAgent, userHostAddress);
        _palaceInfoDictionary.TryGetValue(pi.Key, out var palaceInfo);
        if (palaceInfo == null)
		{
            LoadMicroServices(pi);
            palaceInfo = pi;
            _palaceInfoDictionary.TryAdd(pi.Key, pi);
		}
		else
		{
            palaceInfo.LastHitDate = DateTime.Now;
		}
        return palaceInfo;
    }

    internal Dictionary<string, List<string>> SaveMicroServiceSettings(Models.PalaceInfo palaceInfo, Models.MicroServiceSettings settings)
    {
        var validation = Validate(settings);
        if (!validation.IsValid)
        {
            return validation.BrokenRules;
        }

        Dictionary<string, List<string>> result = null;
        var existing = palaceInfo.MicroServiceSettingsList.FirstOrDefault(i => i.ServiceName == settings.ServiceName);
        if (existing == null)
        {
            result = AddMicroServiceSettings(palaceInfo,settings);
        }
        else
        {
            result = UpdateMicroServiceSettings(palaceInfo,settings);
        }
        return result;
    }

    private Dictionary<string, List<string>> AddMicroServiceSettings(Models.PalaceInfo palaceInfo, Models.MicroServiceSettings settings)
    {
        ((List<Models.MicroServiceSettings>)palaceInfo.MicroServiceSettingsList).Add(settings);
        palaceInfo.LastConfigurationUpdate = DateTime.Now;
        SaveConfiguration(palaceInfo);
        return new Dictionary<string, List<string>>();
    }

    private Dictionary<string, List<string>> UpdateMicroServiceSettings(Models.PalaceInfo palaceInfo, Models.MicroServiceSettings settings)
    {
        var existing = palaceInfo.MicroServiceSettingsList.FirstOrDefault(i => i.ServiceName == settings.ServiceName);

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

        if (existing.InstanceCount != settings.InstanceCount)
        {
            isDirty = true;
        }
        existing.InstanceCount = settings.InstanceCount;

        if (existing.MaxWorkingSetLimitBeforeAlert != settings.MaxWorkingSetLimitBeforeAlert)
        {
            isDirty = true;
        }
        existing.MaxWorkingSetLimitBeforeAlert = settings.MaxWorkingSetLimitBeforeAlert;

        if (existing.MaxWorkingSetLimitBeforeRestart != settings.MaxWorkingSetLimitBeforeRestart)
        {
            isDirty = true;
        }
        existing.MaxWorkingSetLimitBeforeRestart = settings.MaxWorkingSetLimitBeforeRestart;

        if (existing.ThreadLimitBeforeAlert != settings.ThreadLimitBeforeAlert)
        {
            isDirty = true;
        }
        existing.ThreadLimitBeforeAlert = settings.ThreadLimitBeforeAlert;

        if (existing.ThreadLimitBeforeRestart != settings.ThreadLimitBeforeRestart)
        {
            isDirty = true;
        }
        existing.ThreadLimitBeforeRestart = settings.ThreadLimitBeforeRestart;

        if (existing.NotRespondingCountBeforeAlert!= settings.NotRespondingCountBeforeAlert)
        {
            isDirty = true;
        }
        existing.NotRespondingCountBeforeAlert = settings.NotRespondingCountBeforeAlert;

        if (existing.NotRespondingCountBeforeRestart != settings.NotRespondingCountBeforeRestart)
        {
            isDirty = true;
        }
        existing.NotRespondingCountBeforeRestart = settings.NotRespondingCountBeforeRestart;

        if (isDirty)
        {
            palaceInfo.LastConfigurationUpdate = DateTime.Now;
            SaveConfiguration(palaceInfo);
        }
        return new Dictionary<string, List<string>>();
    }

    internal (bool IsValid, Dictionary<string, List<string>> BrokenRules) Validate(Models.MicroServiceSettings mss)
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

    internal void RemoveMicroServiceSettings(Models.PalaceInfo palaceInfo, string serviceName)
    {
        var existing = palaceInfo.MicroServiceSettingsList.FirstOrDefault(i => i.ServiceName == serviceName);
        if (existing != null)
        {
            ((List<Models.MicroServiceSettings>)palaceInfo.MicroServiceSettingsList).Remove(existing);
            palaceInfo.LastConfigurationUpdate = DateTime.Now;
            SaveConfiguration(palaceInfo);
        }
    }

    private void SaveConfiguration(Models.PalaceInfo palaceInfo)
    {
        var configFileName = System.IO.Path.Combine(PalaceServerSettings.MicroServiceConfigurationFolder, $"{palaceInfo.Key}.json");
        var content = System.Text.Json.JsonSerializer.Serialize(palaceInfo.MicroServiceSettingsList, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        });
        if (System.IO.File.Exists(configFileName))
        {
            System.IO.File.Copy(configFileName, $"{configFileName}.bak", true);
        }
        System.IO.File.WriteAllText(configFileName, content);
    }

    private void FillWithUserAgent(Models.PalaceInfo palaceInfo, string userAgent, string userHostAddress)
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

        palaceInfo.Os = match.Groups["os"].Value.Trim();
        palaceInfo.MachineName = match.Groups["machineName"].Value.Trim();
        palaceInfo.HostName = match.Groups["hostName"].Value.Trim();
        palaceInfo.Version = match.Groups["version"].Value.Trim();
        palaceInfo.Ip = userHostAddress;
    }


    private void LoadMicroServices(Models.PalaceInfo palaceInfo)
	{
        var configFileName = System.IO.Path.Combine(PalaceServerSettings.MicroServiceConfigurationFolder, $"{palaceInfo.Key}.json");
        if (System.IO.File.Exists(configFileName))
        {
            var content = System.IO.File.ReadAllText(configFileName);
            var list = System.Text.Json.JsonSerializer.Deserialize<List<Models.MicroServiceSettings>>(content);
            palaceInfo.MicroServiceSettingsList = list;
        }
    }

}
