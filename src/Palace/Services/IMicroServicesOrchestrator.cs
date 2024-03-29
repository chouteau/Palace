﻿using Palace.Configuration;
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
        bool KillProcess(Models.MicroServiceSettings settings);

        MicroServiceInfo GetLocallyInstalledMicroServiceInfo(MicroServiceSettings microServiceSettings);
        Task<RunningMicroserviceInfo> GetRunningMicroServiceInfo(MicroServiceSettings microServiceSettings);

        Task<bool> InstallMicroService(MicroServiceInfo microServiceInfo, Models.MicroServiceSettings serviceSettings);
        Task<bool> UninstallMicroService(Models.MicroServiceSettings serviceSettings);

        Task<bool> UpdateMicroService(MicroServiceInfo microServiceInfo, string packageFileName);

        Task<FileInfoResult> DownloadPackage(string packageFileName);
        Task<AvailablePackage> GetAvailablePackage(MicroServiceSettings microServiceSettings);
        Task RegisterOrUpdateRunningMicroServiceInfo(RunningMicroserviceInfo runningMicroserviceInfo);
        Task UpdateRunningMicroServiceProperty(PalaceServer.Models.ServiceProperties serviceProperties);
        Task<PalaceServer.Models.NextActionResult> GetNextAction(Models.MicroServiceSettings microServiceSettings);
    }
}