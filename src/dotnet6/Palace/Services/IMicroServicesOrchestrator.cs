using Palace.Configuration;
using Palace.Models;
using PalaceClient;
using PalaceServer.Models;

namespace Palace.Services
{
    public interface IMicroServicesOrchestrator
    {
        void StartMicroService(MicroServiceInfo microServiceInfo);
        Task<StopResult> StopRunningMicroService(MicroServiceSettings microServiceSettings);
        bool KillProcess(Models.MicroServiceInfo msi);

        MicroServiceInfo GetLocallyInstalledMicroServiceInfo(MicroServiceSettings microServiceSettings);
        Task<RunningMicroserviceInfo> GetRunningMicroServiceInfo(MicroServiceSettings microServiceSettings);

        Task<bool> InstallMicroService(MicroServiceInfo microServiceInfo, Models.MicroServiceSettings serviceSettings);
        Task UpdateMicroService(MicroServiceInfo microServiceInfo, string packageFileName);
        void BackupMicroServiceFiles(MicroServiceInfo microServiceInfo);

        Task<FileInfoResult> DownloadPackage(string packageFileName);
        Task<AvailablePackage> GetAvailablePackage(MicroServiceSettings microServiceSettings);
        Task RegisterOrUpdateRunningMicroServiceInfo(RunningMicroserviceInfo runningMicroserviceInfo);
        Task UpdateRunningMicroServiceProperty(PalaceServer.Models.ServiceProperties serviceProperties);
        Task<PalaceServer.Models.NextActionResult> GetNextAction(Models.MicroServiceSettings microServiceSettings);
    }
}