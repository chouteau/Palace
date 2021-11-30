using System.Diagnostics;
using System.Net.Http.Json;

namespace Palace
{
    public partial class MicroServicesManager : IMicroServicesManager
    {
        public MicroServicesManager(ILogger<MicroServicesManager> logger,
            IHttpClientFactory httpClientFactory,
            Configuration.PalaceSettings palaceSettings)
        {
            this.Logger = logger;
            this.HttpClientFactory = httpClientFactory;
            this.PalaceSettings = palaceSettings;
        }

        protected ILogger Logger { get; }
        protected IHttpClientFactory HttpClientFactory { get; }
        protected Configuration.PalaceSettings PalaceSettings { get; }

        public void StartMicroService(Models.MicroServiceInfo microServiceInfo)
        {
            Logger.LogInformation($"Try to start {microServiceInfo.Name} with {microServiceInfo.MainFileName}");
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet");

            psi.Arguments = microServiceInfo.MainFileName;
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            psi.RedirectStandardError = true;

            var process = new Process();
            process.StartInfo = psi;

            try
            {
                var start = process.Start();
                if (!start)
                {
                    microServiceInfo.ServiceState = Models.ServiceState.StartFail;
                }
                // var errorStream = process.StandardError;
                // var errorLog = errorStream.ReadToEnd();

                microServiceInfo.Process = process;
                microServiceInfo.ServiceState = Models.ServiceState.Starting;
            }
            catch (Exception ex)
            {
                ex.Data.Add("ServiceName", microServiceInfo.Name);
                ex.Data.Add("ServiceLocation", microServiceInfo.Location);
                Logger.LogError(ex, ex.Message);
            }
        }

        public async Task InstallMicroService(Models.MicroServiceInfo microServiceInfo)
        {
            // On recupere le zip sur le serveur
            var zipFileInfo = await DownloadMicroService(microServiceInfo);
            if (zipFileInfo == null)
            {
                Logger.LogWarning("Download zipfile for service {0} failed", microServiceInfo.Name);
                return;
            }

            var version = 1;
            string extractDirectory = null;
            while (true)
            {
                // Dezip dans son répertoire avec la bonne version
                extractDirectory = System.IO.Path.Combine(PalaceSettings.DownloadDirectory, microServiceInfo.Name, $"v{version}");
                if (Directory.Exists(extractDirectory))
                {
                    version++;
                    continue;
                }
                break;
            }
            Logger.LogWarning("Extact zipfile {0} for service {1} in directory {2}", zipFileInfo.ZipFileName, microServiceInfo.Name, extractDirectory);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFileInfo.ZipFileName, extractDirectory, true);

            // Deploy dans son repertoire d'installation
            await DeployMicroService(microServiceInfo, extractDirectory);
        }

        public async Task UpdateMicroService(Models.MicroServiceInfo microServiceInfo, string zipFileName)
        {
            var version = 1;
            string extractDirectory = null;
            while (true)
            {
                // Dezip dans son répertoire avec la bonne version
                extractDirectory = System.IO.Path.Combine(PalaceSettings.DownloadDirectory, microServiceInfo.Name, $"v{version}");
                if (Directory.Exists(extractDirectory))
                {
                    version++;
                    continue;
                }
                break;
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFileName, extractDirectory, true);

            // Deploy dans son repertoire d'installation
            await DeployMicroService(microServiceInfo, extractDirectory);
        }

        public void BackupMicroServiceFiles(Models.MicroServiceInfo microServiceInfo)
        {
            var fileList = from f in System.IO.Directory.GetFiles(microServiceInfo.Location, "*.*", SearchOption.AllDirectories)
                           select f;

            var backupDirectory = GetNewBackupDirectory(microServiceInfo);
            System.IO.Directory.CreateDirectory(backupDirectory);

            foreach (var file in fileList)
            {
                var destinationFile = file.Replace(microServiceInfo.Location, string.Empty).Trim('\\');
                destinationFile = System.IO.Path.Combine(backupDirectory, destinationFile);
                var destinationDirectory = System.IO.Path.GetDirectoryName(destinationFile);
                if (!System.IO.Directory.Exists(destinationDirectory))
                {
                    System.IO.Directory.CreateDirectory(destinationDirectory);
                }
                System.IO.File.Copy(file, destinationFile);
            }
        }


        public async Task<PalaceClient.RunningMicroserviceInfo> GetRunningMicroServiceInfo(Configuration.MicroServiceSettings microServiceSettings)
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

        public async Task<PalaceClient.StopResult> StopRunningMicroService(Configuration.MicroServiceSettings microServiceSettings)
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

        public Models.MicroServiceInfo GetLocallyInstalledMicroServiceInfo(Configuration.MicroServiceSettings microServiceSettings)
        {
            Models.MicroServiceInfo result = null;
            if (microServiceSettings.MainFileName.StartsWith(@".\"))
            {
                var directoryName = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
                microServiceSettings.MainFileName = System.IO.Path.Combine(directoryName, microServiceSettings.MainFileName.Replace(@".\", ""));
            }
            var fileInfo = new System.IO.FileInfo(microServiceSettings.MainFileName);
            if (!fileInfo.Exists)
            {
                return null;
            }

            result = new Models.MicroServiceInfo
            {
                Location = fileInfo.DirectoryName,
                MainFileName = microServiceSettings.MainFileName,
                Name = microServiceSettings.ServiceName,
                LocalInstallationExists = true,
                LastWriteTime = fileInfo.LastWriteTime
            };

            return result;
        }


        private string GetNewBackupDirectory(Models.MicroServiceInfo microServiceInfo)
        {
            var version = 1;
            string backupDirectory = null;
            while (true)
            {
                backupDirectory = System.IO.Path.Combine(PalaceSettings.BackupDirectory, microServiceInfo.Name, $"v{version}");
                if (System.IO.Directory.Exists(backupDirectory))
                {
                    version++;
                    continue;
                }
                break;
            }
            return backupDirectory;
        }

        private async Task DeployMicroService(Models.MicroServiceInfo microServiceInfo, string unZipFolder)
        {
            var fileList = System.IO.Directory.GetFiles(unZipFolder, "*.*", System.IO.SearchOption.AllDirectories);
            
            Logger.LogInformation($"try to deploy {fileList.Count()} files from {unZipFolder} to {microServiceInfo.Location}");

            foreach (var sourceFile in fileList)
            {
                var destFile = sourceFile.Replace(unZipFolder, "").Trim('\\');
                destFile = System.IO.Path.Combine(microServiceInfo.Location, destFile);

                var destDirectory = System.IO.Path.GetDirectoryName(destFile);
                if (!System.IO.Directory.Exists(destDirectory))
                {
                    System.IO.Directory.CreateDirectory(destDirectory);
                }

                await CopyUpdateFile(sourceFile, destFile);

                if (destFile.Equals(microServiceInfo.MainFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var lastWriteTime = DateTime.Now.AddMinutes(1);
                    File.SetLastWriteTime(destFile, lastWriteTime);
                    microServiceInfo.LastWriteTime = lastWriteTime;
                }

            }

            Logger.LogInformation($"micro service files updated in location {microServiceInfo.Location}");
        }

        private async Task CopyUpdateFile(string sourceFile, string destFile)
        {
            var loop = 0;
            while (true)
            {
                try
                {
                    System.IO.File.Delete(destFile);
                    System.IO.File.Copy(sourceFile, destFile, true);
                    Logger.LogDebug($"Copy {sourceFile} to {destFile}");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    loop++;
                }

                if (loop > 3)
                {
                    break;
                }

                // Le service n'est peut etre pas encore arreté
                await Task.Delay(10 * 1000);
            }
        }
    }
}
