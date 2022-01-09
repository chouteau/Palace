using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public partial class MicroServicesOrchestrator
    {
        public async Task<PalaceClient.RunningMicroserviceInfo> GetRunningMicroServiceInfo(Models.MicroServiceSettings microServiceSettings)
        {
            HttpResponseMessage response = null;
            try
            {
                var httpClient = CreateHttpClient(microServiceSettings);
                var url = $"{microServiceSettings.AdminServiceUrl}/api/palace/infos";
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                ex.Data.Add("url", microServiceSettings.AdminServiceUrl);
                ex.Data.Add("certificate", microServiceSettings.SSLCertificate);
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
            HttpResponseMessage response = null;
            try
            {
                Logger.LogInformation("Try to stop service {0}", microServiceSettings.ServiceName);
                var httpClient = CreateHttpClient(microServiceSettings);
                var url = $"{microServiceSettings.AdminServiceUrl}/api/palace/stop";
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                ex.Data.Add("url", microServiceSettings.AdminServiceUrl);
                ex.Data.Add("certificate", microServiceSettings.SSLCertificate);
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

        public HttpClient CreateHttpClient(Models.MicroServiceSettings microServiceSettings)
        {
            var handler = new HttpClientHandler();
            if (microServiceSettings.AdminServiceUrl.StartsWith("https")
                && !string.IsNullOrWhiteSpace(microServiceSettings.SSLCertificate))
            {
                var certificate = new X509Certificate2(microServiceSettings.SSLCertificate);
                handler.ClientCertificates.Add(certificate);
                handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable)
                    {
                        return false;
                    }
                    return true;
                };
            }
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {microServiceSettings.PalaceApiKey}");
            return httpClient;
        }
    }
}
