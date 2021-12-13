using Palace.Configuration;
using Palace.Models;
using PalaceClient;
using PalaceServer.Models;

namespace Palace
{
    public interface IMicroServicesManager
    {
        void StartMicroService(MicroServiceInfo microServiceInfo);
        Task<StopResult> StopRunningMicroService(MicroServiceSettings microServiceSettings);

        MicroServiceInfo GetLocallyInstalledMicroServiceInfo(MicroServiceSettings microServiceSettings);
        Task<RunningMicroserviceInfo> GetRunningMicroServiceInfo(MicroServiceSettings microServiceSettings);

        Task InstallMicroService(MicroServiceInfo microServiceInfo);
        Task UpdateMicroService(MicroServiceInfo microServiceInfo, string zipFileName);
        void BackupMicroServiceFiles(MicroServiceInfo microServiceInfo);

        Task<DownloadFileInfo> DownloadMicroService(MicroServiceInfo microServiceInfo);
        Task<AvailableMicroServiceInfo> GetAvailableMicroServiceInfo(MicroServiceSettings microServiceSettings);
        Task RegisterOrUpdateRunningMicroServiceInfo(RunningMicroserviceInfo runningMicroserviceInfo);
        Task UpdateRunningMicroServiceProperty(PalaceServer.Models.ServiceProperties serviceProperties);
        Task<PalaceServer.Models.NextActionResult> GetNextAction(Configuration.MicroServiceSettings microServiceSettings);
    }
}