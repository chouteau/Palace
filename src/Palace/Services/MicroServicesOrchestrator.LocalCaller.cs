﻿using System;
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
                ex.Data.Add("serviceName", microServiceSettings.ServiceName);
                Logger.LogError(ex, ex.Message);
                return null;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<PalaceClient.RunningMicroserviceInfo>();
                return result;
            }
            catch(Exception ex)
            {
				ex.Data.Add("url", microServiceSettings.AdminServiceUrl);
				ex.Data.Add("certificate", microServiceSettings.SSLCertificate);
				ex.Data.Add("serviceName", microServiceSettings.ServiceName);
				Logger.LogError(ex, ex.Message);
            }

            return null;
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
            Version httpVersion = null;
            if (microServiceSettings.AdminServiceUrl.StartsWith("https")
                && !string.IsNullOrWhiteSpace(microServiceSettings.SSLCertificate))
            {
                if (System.IO.File.Exists(microServiceSettings.SSLCertificate))
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
                else
                {
                    Logger.LogWarning("SSLCertifiate filename does not exists {0}", microServiceSettings.SSLCertificate);
                }
                httpVersion = new Version(2, 0);
            }
            var httpClient = new HttpClient(handler);
            if (httpVersion != null)
			{
                httpClient.DefaultRequestVersion = httpVersion;
            }
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {microServiceSettings.PalaceApiKey}");
            return httpClient;
        }
    }
}
