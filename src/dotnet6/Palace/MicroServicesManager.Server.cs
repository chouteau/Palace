using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
    public partial class MicroServicesManager
    {
        public async Task<PalaceServer.Models.AvailableMicroServiceInfo> GetAvailableMicroServiceInfo(Configuration.MicroServiceSettings microServiceSettings)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");

            var url = $"{PalaceSettings.UpdateServerUrl}/api/microservices/info/{microServiceSettings.ServiceName}";
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

            var remoteServiceInfo = await response.Content.ReadFromJsonAsync<PalaceServer.Models.AvailableMicroServiceInfo>();
            return remoteServiceInfo;
        }

        public async Task<Models.DownloadFileInfo> DownloadMicroService(Models.MicroServiceInfo microServiceInfo)
        {
            var httpClient = HttpClientFactory.CreateClient("PalaceServer");
            httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);

            var url = $"/api/microservices/download/{microServiceInfo.Name}";
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
                Logger.LogWarning("service {0} response fail for {1}", microServiceInfo.Name, url);
                return null;
            }

            if (!response.Content.Headers.Contains("content-disposition"))
            {
                Logger.LogWarning("service {0} response fail for {1} header content-disposition not found", microServiceInfo.Name, url);
                return null;
            }

            var contentDisposition = response.Content.Headers.GetValues("content-disposition").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(contentDisposition))
            {
                Logger.LogWarning("service {0} response fail for {1} header content-disposition empty", microServiceInfo.Name, url);
                return null;
            }

            if (!System.IO.Directory.Exists(PalaceSettings.DownloadDirectory))
            {
                System.IO.Directory.CreateDirectory(PalaceSettings.DownloadDirectory);
            }

            var result = new Models.DownloadFileInfo();
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

        public async Task<PalaceServer.Models.NextActionResult> GetNextAction(Configuration.MicroServiceSettings microServiceSettings)
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
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PalaceServer.Models.NextActionResult>();
            return result;
        }

    }
}
