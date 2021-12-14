using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Palace
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
        public ILogger Logger { get; }

        public async Task SynchronizeConfiguration()
        {
            var rawJsonConfiguration = System.Text.Json.JsonSerializer.Serialize(PalaceSettings.MicroServiceInfoList);
            var result = await SynchronizeConfiguration(rawJsonConfiguration);
            if (result == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(result.Configuration))
            {
                return;
            }
            if (!result.Configuration.Equals(rawJsonConfiguration))
            {
                Logger.LogInformation("Configuration changed detected");
                var list = System.Text.Json.JsonSerializer.Deserialize<List<Configuration.MicroServiceSettings>>(result.Configuration);
                PalaceSettings.MicroServiceInfoList = list;
                try
                {
                    System.IO.File.Copy(PalaceSettings.PalaceServicesFileName, $"{PalaceSettings.PalaceServicesFileName}.bak", true);
                    System.IO.File.WriteAllText(PalaceSettings.PalaceServicesFileName, result.Configuration);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
            }
        }

        private async Task<PalaceServer.Models.RawJsonConfigurationResult> SynchronizeConfiguration(string rawJsonConfiguration)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);
            HttpResponseMessage response = null;
            try
            {
                var url = $"/api/microservices/synchronize-configuration";
                var httpContent = new StringContent(rawJsonConfiguration, Encoding.UTF8, "text/plain");
                response = await httpClient.PostAsync(url, httpContent);
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

            var result = await response.Content.ReadFromJsonAsync<PalaceServer.Models.RawJsonConfigurationResult>();
            return result;
        }

    }
}
