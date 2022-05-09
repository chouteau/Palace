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
        public async Task<PalaceServer.Models.AvailablePackage> GetAvailablePackage(Models.MicroServiceSettings microServiceSettings)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");

            var url = $"{PalaceSettings.UpdateServerUrl}/api/microservices/info/{microServiceSettings.PackageFileName}";
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                ex.Data.Add("url", url);
                Logger.LogError(ex, ex.Message);
            }

            if (response == null
                || response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            var remoteServiceInfo = await response.Content.ReadFromJsonAsync<PalaceServer.Models.AvailablePackage>();
            return remoteServiceInfo;
        }

        public async Task<Models.FileInfoResult> DownloadPackage(string packageFileName)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);

            var url = $"/api/microservices/download/{packageFileName}";
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return null;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.LogWarning("response fail for download {0}", url);
                return null;
            }

            if (!response.Content.Headers.Contains("content-disposition"))
            {
                Logger.LogWarning("response fail for {0} header content-disposition not found", url);
                return null;
            }

            var contentDisposition = response.Content.Headers.GetValues("content-disposition").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(contentDisposition))
            {
                Logger.LogWarning("response fail for {0} header content-disposition empty", url);
                return null;
            }

            if (!System.IO.Directory.Exists(PalaceSettings.DownloadDirectory))
            {
                System.IO.Directory.CreateDirectory(PalaceSettings.DownloadDirectory);
            }

            var result = new Models.FileInfoResult();
            result.ZipFileName = System.IO.Path.Combine(PalaceSettings.DownloadDirectory, contentDisposition.Split(';')[1].Split('=')[1]);

            if (File.Exists(result.ZipFileName))
            {
                File.Delete(result.ZipFileName);
            }
            using (var fs = new System.IO.FileStream(result.ZipFileName, System.IO.FileMode.Create))
            {
                var stream = response.Content.ReadAsStreamAsync().Result;
                int bufferSize = 1024;
                byte[] buffer = new byte[bufferSize];
                int pos = 0;
                while ((pos = stream.Read(buffer, 0, bufferSize)) > 0)
                {
                    fs.Write(buffer, 0, pos);
                }
                fs.Close();
            }

            return result;
        }

        public async Task RegisterOrUpdateRunningMicroServiceInfo(PalaceClient.RunningMicroserviceInfo runningMicroserviceInfo)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);

            var url = $"/api/microservices/registerorupdateinfos";
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.PostAsJsonAsync(url, runningMicroserviceInfo);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        public async Task UpdateRunningMicroServiceProperty(PalaceServer.Models.ServiceProperties serviceProperties)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);

            var url = $"/api/microservices/updateserviceproperties";
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.PostAsJsonAsync(url, serviceProperties);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        public async Task<PalaceServer.Models.NextActionResult> GetNextAction(Models.MicroServiceSettings microServiceSettings)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);
            HttpResponseMessage response = null;
            try
            {
                var url = $"/api/microservices/getnextaction/{microServiceSettings.ServiceName}";
                response = await httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return null;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.LogWarning($"get nextaction for {microServiceSettings.ServiceName} return bad status code {response.StatusCode}");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PalaceServer.Models.NextActionResult>();
            Logger.LogDebug($"Next action : {result.Action}");
            return result;
        }

    }
}
