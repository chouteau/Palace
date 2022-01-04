using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public partial class MicroServicesOrchestrator
    {
        public async Task<PalaceClient.RunningMicroserviceInfo> GetRunningMicroServiceInfo(Models.MicroServiceSettings microServiceSettings)
        {
            var httpClient = HttpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {microServiceSettings.PalaceApiKey}");
            HttpResponseMessage response = null;
            try
            {
                var url = $"{microServiceSettings.AdminServiceUrl}/api/palace/infos";
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

            var result = await response.Content.ReadFromJsonAsync<PalaceClient.RunningMicroserviceInfo>();
            return result;
        }

        public async Task<PalaceClient.StopResult> StopRunningMicroService(Models.MicroServiceSettings microServiceSettings)
        {
            var httpClient = HttpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {microServiceSettings.PalaceApiKey}");
            HttpResponseMessage response = null;
            try
            {
                Logger.LogInformation("Try to stop service {0}", microServiceSettings.ServiceName);
                var url = $"{microServiceSettings.AdminServiceUrl}/api/palace/stop";
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return null;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.LogWarning("Stop service {0} failed", microServiceSettings.ServiceName);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PalaceClient.StopResult>();
            return result;
        }

    }
}
