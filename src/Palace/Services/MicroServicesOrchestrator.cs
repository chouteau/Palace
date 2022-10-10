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
            var deploySuccess = await DeployMicroService(microServiceInfo, extractDirectory);
            return deploySuccess;
        }

        public Task<bool> UninstallMicroService(Models.MicroServiceSettings microServiceSettings)
        {
            bool uninstallSuccess = true;
            var directory = System.IO.Path.Combine(PalaceSettings.InstallationDirectory, microServiceSettings.ServiceName);
            var fileList = System.IO.Directory.GetFiles(directory, "*.*", System.IO.SearchOption.AllDirectories);
            var directoryList = System.IO.Directory.GetDirectories(directory, "*.*", SearchOption.AllDirectories);

            Logger.LogInformation($"try to remove {fileList.Count()} files from {microServiceSettings.InstallationFolder}");

            foreach (var removeFile in fileList)
            {
                try
                {
                    System.IO.File.Delete(removeFile);
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    uninstallSuccess = false;
                }
            }
            foreach (var removeDirectory in directoryList)
            {
                try
                {
                    System.IO.Directory.Delete(removeDirectory);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    uninstallSuccess = false;
                }
            }

            try
			{
                System.IO.Directory.Delete(directory, true);
            }
            catch(Exception ex)
			{
                ex.Data.Add("Directory", directory);
                Logger.LogError(ex, ex.Message);
                uninstallSuccess = false;
            }

            return Task.FromResult(uninstallSuccess);
        }

        public async Task<bool> UpdateMicroService(Models.MicroServiceInfo microServiceInfo, string packageFileName)
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
            var deploySuccess = await DeployMicroService(microServiceInfo, extractDirectory);
            return deploySuccess;
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

        public bool KillProcess(Models.MicroServiceSettings settings)
        {
            return true;
			var processList = Process.GetProcesses();
			if (!processList.Any())
			{
				Logger.LogWarning("Could not find any dotnet.exe process in this machine");
				return false;
			}
            try
            {
				foreach (var p in processList)
				{
                    if (p.ProcessName.IndexOf("dotnet") != -1)
					{
                        string arguments = null;
                        try
						{
                            arguments = p.StartInfo.Arguments;
                        }
                        catch
						{

						}
						Logger.LogWarning("Try to kill process {0}", p.Id);
					}
                    Logger.LogDebug(p.ProcessName);
				}
                var process = processList.FirstOrDefault(p => p.StartInfo.Arguments.Contains(settings.MainAssembly));
                process.Kill(true);
                Logger.LogWarning("Try to kill service {0} on processId {1}", settings.ServiceName, process.Id);
                process.Kill(true);
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


        private async Task<bool> DeployMicroService(Models.MicroServiceInfo microServiceInfo, string unZipFolder)
        {
            var deploySuccess = true;
            var fileList = System.IO.Directory.GetFiles(unZipFolder, "*.*", System.IO.SearchOption.AllDirectories);
            
            Logger.LogInformation($"try to deploy {fileList.Count()} files from {unZipFolder} to {microServiceInfo.InstallationFolder}");

            try
            {
                // Nettoyage global du repertoire de destination
                System.IO.Directory.Delete(microServiceInfo.InstallationFolder, true);
                System.IO.Directory.CreateDirectory(microServiceInfo.InstallationFolder);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return false;
            }

			foreach (var sourceFile in fileList)
            {
                var destFile = sourceFile.Replace(unZipFolder, "").Trim('\\');
                if (string.IsNullOrWhiteSpace(microServiceInfo.InstallationFolder))
				{
                    Logger.LogWarning($"Installation folder not defined for service {microServiceInfo.Name}");
                    continue;
				}
                destFile = System.IO.Path.Combine(microServiceInfo.InstallationFolder,  destFile);

                var destDirectory = System.IO.Path.GetDirectoryName(destFile);
                if (!System.IO.Directory.Exists(destDirectory))
                {
                    System.IO.Directory.CreateDirectory(destDirectory);
                }

                var isCopySuccess = await CopyUpdateFile(sourceFile, destFile);
                if (!isCopySuccess)
                {
                    deploySuccess = false;
                    break;
                }

                if (System.IO.Path.GetFileName(destFile).Equals(microServiceInfo.MainFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var lastWriteTime = DateTime.Now.AddSeconds(PalaceSettings.ScanIntervalInSeconds + 1);
                    File.SetLastWriteTime(destFile, lastWriteTime);
                    microServiceInfo.LastWriteTime = lastWriteTime;
                }

            }

            if (deploySuccess)
            {
                Logger.LogInformation($"micro service files updated in location {microServiceInfo.InstallationFolder}");
            }
            else
            {
                Logger.LogInformation("deploy failed for service {0}", microServiceInfo.InstallationFolder);
            }

            return deploySuccess;
        }

        private async Task<bool> CopyUpdateFile(string sourceFile, string destFile)
        {
            bool copySuccess = true;
            var loop = 0;
            while (true)
            {
                try
                {
                    System.IO.File.Delete(destFile);
                    System.IO.File.Copy(sourceFile, destFile, true);
                    Logger.LogDebug($"Copy {sourceFile} to {destFile}");
                    copySuccess = true;
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    loop++;
                    copySuccess = false;
                }

                if (loop > 3)
                {
                    break;
                }

                // Le service n'est peut etre pas encore arreté
                await Task.Delay(2 * 1000);
            }

            return copySuccess;
        }
    }
}
