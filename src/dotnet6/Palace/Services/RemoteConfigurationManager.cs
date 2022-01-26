using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    internal class RemoteConfigurationManager : IRemoteConfigurationManager
    {
        public RemoteConfigurationManager(Configuration.PalaceSettings palaceSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<RemoteConfigurationManager> logger)
        {
            this.PalaceSettings = palaceSettings;
            this.HttpClientFactory = httpClientFactory;
            this.Logger = logger;   
        }

        protected Configuration.PalaceSettings PalaceSettings { get; }
        protected IHttpClientFactory HttpClientFactory { get; } 
        protected ILogger Logger { get; }


        public async Task<IEnumerable<Models.MicroServiceSettings>> SynchronizeConfiguration(IEnumerable<Models.MicroServiceSettings> list)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);
            HttpResponseMessage response = null;

            var configFileInfo = new System.IO.FileInfo(PalaceSettings.PalaceServicesFileName);
            try
            {
                var url = $"/api/microservices/synchronize-configuration";
                httpClient.DefaultRequestHeaders.IfModifiedSince = configFileInfo.LastWriteTime;
                response = await httpClient.PostAsJsonAsync(url, list);
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

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<Models.MicroServiceSettings>>();
            return result;
        }

    }
}
