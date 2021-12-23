using System.Diagnostics;
using System.Net.Http.Json;

namespace Palace.Services
{
    public partial class MicroServicesOrchestrator : IMicroServicesOrchestrator
    {
        public MicroServicesOrchestrator(ILogger<MicroServicesOrchestrator> logger,
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

            var mainFileName = System.IO.Path.Combine(PalaceSettings.InstallationDirectory, microServiceInfo.Name, microServiceInfo.MainFileName);
            psi.Arguments = $"{mainFileName} {microServiceInfo.Arguments}".Trim();
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            psi.RedirectStandardError = true;
            psi.ErrorDialog = false;

            var process = new Process();
            process.StartInfo = psi;
            process.EnableRaisingEvents = true;
            process.ErrorDataReceived += (s, arg) =>
            {
                if (string.IsNullOrWhiteSpace(arg.Data))
                {
                    return;
                }
                Logger.LogCritical(arg.Data);
                microServiceInfo.ServiceState = Models.ServiceState.StartFail;
                microServiceInfo.StartFailedMessage = arg.Data;
            };

            try
            {
                var start = process.Start();
                if (!start)
                {
                    microServiceInfo.ServiceState = Models.ServiceState.StartFail;
                }
                process.BeginErrorReadLine();

                microServiceInfo.Process = process;
                microServiceInfo.ServiceState = Models.ServiceState.Starting;
            }
            catch (Exception ex)
            {
                ex.Data.Add("ServiceName", microServiceInfo.Name);
                ex.Data.Add("ServiceLocation", microServiceInfo.InstallationFolder);
                Logger.LogError(ex, ex.Message);
            }
        }

        public async Task<bool> InstallMicroService(Models.MicroServiceInfo microServiceInfo, Models.MicroServiceSettings serviceSettings)
        {
            microServiceInfo.InstallationFolder = System.IO.Path.Combine(PalaceSettings.InstallationDirectory, serviceSettings.ServiceName);
            microServiceInfo.MainFileName = serviceSettings.MainAssembly;
            microServiceInfo.Arguments = serviceSettings.Arguments;

            Logger.LogInformation("Try to install MicroService{0} in {1}", microServiceInfo.MainFileName, microServiceInfo.InstallationFolder);
            // On recupere le zip sur le serveur
            var zipFileInfo = await DownloadPackage(serviceSettings.PackageFileName);
            if (zipFileInfo == null)
            {
                Logger.LogWarning("Download zipfile for service {0} failed", microServiceInfo.Name);
                return false;
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
            if (!System.IO.Directory.Exists(extractDirectory))
            {
                System.IO.Directory.CreateDirectory(extractDirectory);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFileInfo.ZipFileName, extractDirectory, true);

            // Deploy dans son repertoire d'installation
            await DeployMicroService(microServiceInfo, extractDirectory);
            return true;
        }

        public async Task UpdateMicroService(Models.MicroServiceInfo microServiceInfo, string packageFileName)
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
            System.IO.Compression.ZipFile.ExtractToDirectory(packageFileName, extractDirectory, true);

            // Deploy dans son repertoire d'installation
            await DeployMicroService(microServiceInfo, extractDirectory);
        }

        public void BackupMicroServiceFiles(Models.MicroServiceInfo microServiceInfo)
        {
            if (!System.IO.Directory.Exists(microServiceInfo.InstallationFolder))
            {
                // Le service n'est pas encore installé
                Logger.LogInformation("No backup for not already installed service {0}", microServiceInfo.Name);
                return;
            }

            var fileList = from f in System.IO.Directory.GetFiles(microServiceInfo.InstallationFolder, "*.*", SearchOption.AllDirectories)
                           select f;

            var backupDirectory = GetNewBackupDirectory(microServiceInfo);
            System.IO.Directory.CreateDirectory(backupDirectory);

            foreach (var file in fileList)
            {
                var destinationFile = file.Replace(microServiceInfo.InstallationFolder, string.Empty).Trim('\\');
                destinationFile = System.IO.Path.Combine(backupDirectory, destinationFile);
                var destinationDirectory = System.IO.Path.GetDirectoryName(destinationFile);
                if (!System.IO.Directory.Exists(destinationDirectory))
                {
                    System.IO.Directory.CreateDirectory(destinationDirectory);
                }
                System.IO.File.Copy(file, destinationFile);
            }
        }

        public bool KillProcess(Models.MicroServiceInfo msi)
        {
            if (msi == null
                || msi.Process == null)
            {
                return true;
            }
            try
            {
                Logger.LogWarning("Try to kill service {0} on processId {1}", msi.Name, msi.Process.Id);
                msi.Process.Kill(true);
                msi.Process = null;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
            return false;
        }

        public Models.MicroServiceInfo GetLocallyInstalledMicroServiceInfo(Models.MicroServiceSettings microServiceSettings)
        {
            Models.MicroServiceInfo result = null;
            var directoryName = System.IO.Path.Combine(PalaceSettings.InstallationDirectory, microServiceSettings.ServiceName);
            var mainAssemblyFileName = System.IO.Path.Combine(directoryName, microServiceSettings.MainAssembly.Replace(@".\", ""));

            var fileInfo = new System.IO.FileInfo(mainAssemblyFileName);
            if (!fileInfo.Exists)
            {
                return null;
            }

            result = new Models.MicroServiceInfo
            {
                InstallationFolder = directoryName,
                MainFileName =  microServiceSettings.MainAssembly,
                Arguments = microServiceSettings.Arguments,
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
            
            Logger.LogInformation($"try to deploy {fileList.Count()} files from {unZipFolder} to {microServiceInfo.InstallationFolder}");

            foreach (var sourceFile in fileList)
            {
                var destFile = sourceFile.Replace(unZipFolder, "").Trim('\\');
                destFile = System.IO.Path.Combine(microServiceInfo.InstallationFolder,  destFile);

                var destDirectory = System.IO.Path.GetDirectoryName(destFile);
                if (!System.IO.Directory.Exists(destDirectory))
                {
                    System.IO.Directory.CreateDirectory(destDirectory);
                }

                await CopyUpdateFile(sourceFile, destFile);

                if (System.IO.Path.GetFileName(destFile).Equals(microServiceInfo.MainFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var lastWriteTime = DateTime.Now.AddSeconds(10);
                    File.SetLastWriteTime(destFile, lastWriteTime);
                    microServiceInfo.LastWriteTime = lastWriteTime;
                }

            }

            Logger.LogInformation($"micro service files updated in location {microServiceInfo.InstallationFolder}");
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
