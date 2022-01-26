using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public class MicroServicesCollectionManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.MicroServiceSettings> _list;

        public MicroServicesCollectionManager(Configuration.PalaceSettings palaceSettings,
            ILogger<MicroServicesCollectionManager> logger,
            IRemoteConfigurationManager remoteConfigurationManager)
        {
            this.PalaceSettings = palaceSettings;
            this.Logger = logger;
            this.RemoteConfigurationManager = remoteConfigurationManager;   
            _list = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.MicroServiceSettings>();
            try
            {
                BindFromFileName();
            }
            catch (Exception ex)
            {
                Logger.LogCritical("Bind micro services collection from fileName fail with error : {0}", ex.Message);
            }
        }

        protected Configuration.PalaceSettings PalaceSettings { get; }
        protected ILogger Logger { get;  }
        protected IRemoteConfigurationManager RemoteConfigurationManager { get; }  

        internal void BindFromFileName()
        {
            if (System.IO.File.Exists(PalaceSettings.PalaceServicesFileName))
            {
                var content = System.IO.File.ReadAllText(PalaceSettings.PalaceServicesFileName);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<Models.MicroServiceSettings>>(content);
                foreach (var item in list)
                {
                    AddOrUpdate(item);                    
                }
            }
        }

        public void AddOrUpdate(Models.MicroServiceSettings microServiceSettings)
        {
            if (microServiceSettings == null)
            {
                Logger.LogWarning("microServiceSettings is null");
                return;
            }

            var validate = Validate(microServiceSettings);
            if (!validate.IsValid)
            {
                Logger.LogWarning("microServiceSettings {0} is invalid\r{1}", microServiceSettings.ServiceName, string.Join("\r", validate.BrokenRules));
                return;
            }

            if (_list.Keys.Any(i => i.Equals(microServiceSettings.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
            {
                _list[microServiceSettings.ServiceName] = microServiceSettings;
                return;
            }

            _list.TryAdd(microServiceSettings.ServiceName, microServiceSettings);
        }

        public void Remove(Models.MicroServiceSettings microServiceSettings)
        {
            if (_list.Keys.Any(i => i.Equals(microServiceSettings.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
            {
                _list.TryRemove(microServiceSettings.ServiceName, out var remove);
                Logger.LogInformation("service {0} removed", remove.ServiceName);
            }
        }

        public IEnumerable<Models.MicroServiceSettings> GetList()
        { 
            return _list.Values;
        }

        internal (bool IsValid, List<string> BrokenRules) Validate(Models.MicroServiceSettings mss)
        {
            var result = true;
            var brokenRules = new List<string>();
            if (string.IsNullOrWhiteSpace(mss.ServiceName))
            {
                brokenRules.Add("Service name is null or empty");
                result = false;
            }
            else if (_list.Any(i => i.Key.Equals(mss.ServiceName, StringComparison.InvariantCultureIgnoreCase)))
            {
                brokenRules.Add("Service name already exists");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(mss.MainAssembly))
            {
                brokenRules.Add("MainAssembly is null or empty");
                result = false;
            }
            if (string.IsNullOrWhiteSpace(mss.PackageFileName))
            {
                brokenRules.Add("PackageFileName is null or empty");
                result = false;
            }
            else if (!mss.PackageFileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                brokenRules.Add("PackageFileName is not zip file");
                result = false;
            }
            if (string.IsNullOrWhiteSpace(mss.AdminServiceUrl))
            {
                brokenRules.Add("AdminServiceUrl is null or empty");
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
                    brokenRules.Add("AdminServiceUrl is not uri");
                    result = false;
                }
            }


            return (result, brokenRules);
        }

        public async Task SynchronizeConfiguration()
        {
            var mslist = GetList();
            var result = await RemoteConfigurationManager.SynchronizeConfiguration(mslist);
            if (result == null)
            {
                return;
            }

            var isDirty = false;

            var servicesToDelete = mslist.Select(i => i.ServiceName)
                                .Except(result.Select(i => i.ServiceName))
                                .ToList();

            var servicesToAdd = result.Select(i => i.ServiceName)
                                .Except(mslist.Select(i => i.ServiceName))
                                .ToList();

            // Existing
            foreach (var existing in mslist)
            {
                var remote = result.SingleOrDefault(i => i.ServiceName == existing.ServiceName);
                if (remote == null)
                {
                    AddOrUpdate(remote);
                    continue;
                }
                var deco = new Models.MicroServiceSettingsDecorator(existing);
                deco.AdminServiceUrl = remote.AdminServiceUrl;
                deco.ServiceName = remote.ServiceName;
                deco.AlwaysStarted = remote.AlwaysStarted;
                deco.MainAssembly = remote.MainAssembly;
                deco.PalaceApiKey = remote.PalaceApiKey;
                deco.SSLCertificate = remote.SSLCertificate;
                deco.Arguments = remote.Arguments;
                deco.InstallationFolder = remote.InstallationFolder;
                deco.PackageFileName = remote.PackageFileName;

                if (deco.IsDirty)
                {
                    AddOrUpdate(remote);
                    isDirty = true;
                }
            }

            foreach (var serviceName in servicesToAdd)
            {
                var serviceToAdd = result.SingleOrDefault(i => i.ServiceName == serviceName);
                if (serviceToAdd != null)
                {
                    AddOrUpdate(serviceToAdd);
                    isDirty = true;
                }
            }

            foreach (var delete in servicesToDelete)
            {
                var existing = mslist.SingleOrDefault(i => i.ServiceName == delete);
                if (existing != null)
                {
                    existing.MarkToDelete = true;
                }
            }

            if (isDirty)
            {
                Logger.LogInformation("Configuration changed detected");
                SaveConfiguration();
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                System.IO.File.Copy(PalaceSettings.PalaceServicesFileName, $"{PalaceSettings.PalaceServicesFileName}.bak", true);
                var content = System.Text.Json.JsonSerializer.Serialize(GetList(), new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                });
                System.IO.File.WriteAllText(PalaceSettings.PalaceServicesFileName, content);
                Logger.LogInformation("Configuration file saved {0}", PalaceSettings.PalaceServicesFileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

    }
}
