using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public class MicroServicesCollectionManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Models.MicroServiceSettings> _list;

        public MicroServicesCollectionManager(Configuration.PalaceSettings palaceSettings,
            ILogger<MicroServicesCollectionManager> logger,
            IHttpClientFactory httpClientFactory)
        {
            this.PalaceSettings = palaceSettings;
            this.Logger = logger;
            this.HttpClientFactory = httpClientFactory;
            _list = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.MicroServiceSettings>();
        }

        protected Configuration.PalaceSettings PalaceSettings { get; }
        protected ILogger Logger { get;  }
        protected IHttpClientFactory HttpClientFactory { get; }

        public IEnumerable<Models.MicroServiceSettings> GetList()
        {
            return _list.Values;
        }

        public void Remove(Models.MicroServiceSettings item)
        {
            _list.TryRemove(item.ServiceName, out var removed);
        }

        public async Task SynchronizeConfiguration()
        {
            var result = await GetConfiguration();
            if (result == null)
            {
                return;
            }

            var isDirty = false;

            var servicesToDelete = _list.Select(i => i.Key)
                                .Except(result.Select(i => i.ServiceName))
                                .ToList();

            var servicesToAdd = result.Select(i => i.ServiceName)
                                .Except(_list.Select(i => i.Key))
                                .ToList();

            // Existing
            foreach (var existing in _list)
            {
                var remote = result.SingleOrDefault(i => i.ServiceName == existing.Key);
                if (remote == null)
                {
                    continue;
                }
                existing.Value.AdminServiceUrl = remote.AdminServiceUrl;
                existing.Value.ServiceName = remote.ServiceName;
                existing.Value.AlwaysStarted = remote.AlwaysStarted;
                existing.Value.MainAssembly = remote.MainAssembly;
                existing.Value.PalaceApiKey = remote.PalaceApiKey;
                existing.Value.SSLCertificate = remote.SSLCertificate;
                existing.Value.Arguments = remote.Arguments;
                existing.Value.InstallationFolder = remote.InstallationFolder;
                existing.Value.PackageFileName = remote.PackageFileName;
            }

            foreach (var serviceName in servicesToAdd)
            {
                var serviceToAdd = result.SingleOrDefault(i => i.ServiceName == serviceName);
                if (serviceToAdd != null)
                {
                    _list.TryAdd(serviceName, serviceToAdd);
                    isDirty = true;
                }
            }

            foreach (var delete in servicesToDelete)
            {
                var existing = _list.SingleOrDefault(i => i.Key == delete);
                if (existing.Key != null)
                {
                    existing.Value.MarkToDelete = true;
                }
            }

            if (isDirty)
            {
                Logger.LogInformation("Configuration changed detected");
            }
        }

        private async Task<IEnumerable<Models.MicroServiceSettings>> GetConfiguration()
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);
            HttpResponseMessage response = null;

            try
            {
                var url = $"/api/microservices/configuration";
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return null;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<IEnumerable<Models.MicroServiceSettings>>();
                return result;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }

            return new List<Models.MicroServiceSettings>();
        }


    }
}
