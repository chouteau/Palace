using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
    public class Starter : IStarter
    {
        public Starter(Configuration.PalaceSettings palaceSettings,
            ILogger<Starter> logger,
            IMicroServicesManager serviceManager,
            IMemoryCache cache,
            IAlertNotification alertNotification)
        {
            this.PalaceSettings = palaceSettings;
            this.Logger = logger;
            this.ServiceManager = serviceManager;
            this.Cache = cache;
            this.AlertNotification = alertNotification;
        }

        protected Configuration.PalaceSettings PalaceSettings { get; }
        protected ILogger Logger { get; }
        protected IMicroServicesManager ServiceManager { get; }
        protected IMemoryCache Cache { get; }
        protected IAlertNotification AlertNotification { get; }
        protected List<Models.MicroServiceInfo> InstanciedServiceList
        {
            get
            {
                var cacheKey = "instanciedServiceList";
                Cache.TryGetValue(cacheKey, out List<Models.MicroServiceInfo> list);
                if (list == null)
                {
                    list = new List<Models.MicroServiceInfo>();
                    Cache.Set(cacheKey, list, new MemoryCacheEntryOptions
                    {
                        Priority = CacheItemPriority.High,
                        AbsoluteExpiration = DateTime.Now.AddYears(100)
                    });
                }
                return list;
            }
        }

        public int InstanciedServiceCount
        {
            get
            {
                return InstanciedServiceList.Count;
            }
        }

        public async Task Start()
        {
            Logger.LogInformation($"Starting {PalaceSettings.MicroServiceInfoList.Count} micro services");
            foreach (var serviceSettings in PalaceSettings.MicroServiceInfoList)
            {
                Logger.LogInformation($"Try to start {serviceSettings.ServiceName}");
                while (true)
                {
                    var serviceInfo = await StartMicroService(serviceSettings);

                    // Tout est ok
                    if (serviceInfo.ServiceState == Models.ServiceState.Starting
                        || serviceInfo.ServiceState == Models.ServiceState.Started)
                    {
                        break;
                    }

                    // Pas la peine de contiuer le service n'existe pas
                    if (serviceInfo.ServiceState == Models.ServiceState.NotExists)
                    {
                        Logger.LogWarning("this service {0} does not exists", serviceInfo.Name);
                        break;
                    }

                    // On attend la mise à jour
                    if (serviceInfo.ServiceState == Models.ServiceState.UpdateInProgress)
                    {
                        Logger.LogInformation("Waiting update for this service {0}", serviceInfo.Name);
                        await Task.Delay(PalaceSettings.WaitingUpdateTimeoutInSecond * 1000);
                        continue;
                    }

                    // Demarrage du service si demarrage obligatoire
                    if (serviceInfo.ServiceState == Models.ServiceState.Offline
                        && serviceSettings.AlwaysStarted)
                    {
                        Logger.LogInformation("Waiting for start this service {0}", serviceInfo.Name);
                        await Task.Delay(5 * 1000);
                        continue;
                    }

                    if (serviceInfo.ServiceState == Models.ServiceState.NotInstalled)
                    {
                        serviceInfo.Location = System.IO.Path.GetDirectoryName(serviceSettings.MainFileName);
                        serviceInfo.MainFileName = serviceSettings.MainFileName;
                        Logger.LogInformation("Install this service {0}", serviceInfo.Name);
                        await ServiceManager.InstallMicroService(serviceInfo);
                        await Task.Delay(10 * 1000);
                        continue;
                    }

                    break;
                }
            }
        }

        public async Task<bool> ApplyAction()
        {
            foreach (var item in PalaceSettings.MicroServiceInfoList)
            {
                var actionResult = await ServiceManager.GetNextAction(item);
                switch (actionResult?.Action)
                {
                    case PalaceServer.Models.ServiceAction.Start:
                        Logger.LogInformation("try to start {0}", item.ServiceName);
                        var startResult = await StartMicroService(item);
                        if (startResult == null)
                        {
                            Logger.LogWarning("start {0} fail", item.ServiceName);
                        }
                        return true;

                    case PalaceServer.Models.ServiceAction.Stop:
                        await StopMicroService(item);
                        return true;
                    default:
                        return false;
                }
            }
            return false;
        }
        public async Task CheckHealth()
        {
            foreach (var item in PalaceSettings.MicroServiceInfoList)
            {
                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));
                if (instancied == null)
                {
                    continue;
                }
                var info = await ServiceManager.GetRunningMicroServiceInfo(item);
                if (info == null)
                {
                    var message = $"Service {item.ServiceName} not responding @ {item.AdminServiceUrl}";
                    AlertNotification.Notify(message);
                    instancied.ServiceState = Models.ServiceState.NotResponding;
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                    await ServiceManager.UpdateRunningMicroServiceProperty(sps);
                    continue;
                }
                instancied.ServiceState = Models.ServiceState.Started;
                info.ServiceState = $"{instancied.ServiceState}";
                if (instancied.Process != null)
                {
                    info.PeakWorkingSet = instancied.Process.PeakWorkingSet64;
                    info.PeakVirtualMem = instancied.Process.PeakVirtualMemorySize64;
                    info.PeakPagedMem = instancied.Process.PeakPagedMemorySize64;
                    info.StartedDate = instancied.Process.StartTime;
                }
                await ServiceManager.RegisterOrUpdateRunningMicroServiceInfo(info);
                Logger.LogDebug("service {0} is up", info.ServiceName);
            }
        }

        public async Task CheckUpdate()
        {
            foreach (var item in PalaceSettings.MicroServiceInfoList)
            {
                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));
                if (instancied == null)
                {
                    continue;
                }

                if (instancied.ServiceState == Models.ServiceState.UpdateInProgress)
                {
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                    await ServiceManager.UpdateRunningMicroServiceProperty(sps);
                    continue;
                }

                var remoteServiceInfo = await ServiceManager.GetAvailableMicroServiceInfo(item);
                var totalMinute = (instancied.LastWriteTime.GetValueOrDefault(DateTime.MinValue) - remoteServiceInfo.LastWriteTime).TotalMinutes;
                if (totalMinute <= 0)
                {
                    Logger.LogInformation("Update detected for service {0}", instancied.Name);
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.UpdateDetected}");
                    await ServiceManager.UpdateRunningMicroServiceProperty(sps);

                    instancied.ServiceState = Models.ServiceState.UpdateInProgress;

                    ServiceManager.BackupMicroServiceFiles(instancied);

                    var update = await ServiceManager.DownloadMicroService(instancied);
                    if (update != null)
                    {
                        var stopResult = await ServiceManager.StopRunningMicroService(item);
                        if (stopResult == null)
                        {
                            Logger.LogCritical("Stop service impossible");
                            continue;
                        }
                        if (stopResult.Message == "fail")
                        {
                            Logger.LogCritical($"Stop service with messsage {stopResult.Message}");
                        }

                        await Task.Delay(5 * 1000);

                        if (instancied.Process == null
                            || instancied.Process.HasExited)
                        {
                            instancied.ServiceState = Models.ServiceState.Offline;
                            instancied.Process = null;

                            sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                            await ServiceManager.UpdateRunningMicroServiceProperty(sps);

                            await ServiceManager.UpdateMicroService(instancied, update.ZipFileName);

                            sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.Updated}");
                            await ServiceManager.UpdateRunningMicroServiceProperty(sps);

                            ServiceManager.StartMicroService(instancied);
                        }
                    }
                }

            }
        }

        public async Task Stop()
        {
            Logger.LogInformation("Stop services");
            if (!PalaceSettings.StopAllMicroServicesWhenStop)
            {
                return;
            }

            Logger.LogInformation($"Stoping {PalaceSettings.MicroServiceInfoList.Count} micro services");
            foreach (var serviceSettings in PalaceSettings.MicroServiceInfoList)
            {
                await StopMicroService(serviceSettings);
            }
        }

        private async Task StopMicroService(Configuration.MicroServiceSettings serviceSettings)
        {
            Logger.LogInformation("try to stop {0}", serviceSettings.ServiceName);
            var result = await ServiceManager.StopRunningMicroService(serviceSettings);
            if (result != null)
            {
                Logger.LogInformation("stop {0} with result {1}", serviceSettings.ServiceName, result.Message);
            }

            await Task.Delay(2 * 1000);

            var instantiedService = InstanciedServiceList.SingleOrDefault(i => i.Name == serviceSettings.ServiceName);
            if (instantiedService != null
                && instantiedService.Process != null
                    && instantiedService.Process.HasExited)
            {
                InstanciedServiceList.Remove(instantiedService);
            }

            var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(serviceSettings.ServiceName, $"{Models.ServiceState.Offline}");
            await ServiceManager.UpdateRunningMicroServiceProperty(sps);
        }

        private async Task<Models.MicroServiceInfo> StartMicroService(Configuration.MicroServiceSettings serviceSettings)
        {
            // Verification si l'executable est présent
            var serviceInfo = ServiceManager.GetLocallyInstalledMicroServiceInfo(serviceSettings);
            if (serviceInfo == null)
            {
                // S'il 'est pas présent localement, on verifie s'il est présent sur le serveur
                var remoteServiceInfo = await ServiceManager.GetAvailableMicroServiceInfo(serviceSettings);
                if (remoteServiceInfo == null)
                {
                    serviceInfo = new Models.MicroServiceInfo();
                    serviceInfo.Name = serviceSettings.ServiceName;
                    serviceInfo.ServiceState = Models.ServiceState.NotExists;
                    Logger.LogWarning("this service does not exists {0}", serviceSettings.ServiceName);
                }
                else
                {
                    serviceInfo = new Models.MicroServiceInfo();
                    serviceInfo.Name = remoteServiceInfo.ServiceName;
                    serviceInfo.ServiceState = Models.ServiceState.NotInstalled;
                }
            }
            else
            {
                serviceInfo.LocalInstallationExists = true;
                // Verification si le service est déjà en ligne
                var runningServiceInfo = await ServiceManager.GetRunningMicroServiceInfo(serviceSettings);
                if (runningServiceInfo != null)
                {
                    serviceInfo.ServiceState = Models.ServiceState.Started;
                    if (runningServiceInfo.ProcessId > 0)
                    {
                        var pid = System.Diagnostics.Process.GetProcessById(runningServiceInfo.ProcessId);
                        serviceInfo.Process = pid;
                    }
                }
            }

            if (serviceInfo.ServiceState == Models.ServiceState.Started)
            {
                Logger.LogWarning("this micro service {0} is already started", serviceSettings.ServiceName);
                await AddInstantiatedService(serviceInfo);
            }
            else if (serviceInfo.LocalInstallationExists)
            {
                // Demarrage du service
                ServiceManager.StartMicroService(serviceInfo);
                if (serviceInfo.Process == null)
                {
                    Logger.LogWarning("service not started {0}", serviceSettings.ServiceName);
                }
                else
                {
                    await AddInstantiatedService(serviceInfo);
                }
            }

            return serviceInfo;
        }

        private async Task AddInstantiatedService(Models.MicroServiceInfo serviceInfo)
        {
            if (!InstanciedServiceList.Any(i => i.Name == serviceInfo.Name))
            {
                Logger.LogInformation($"micro service {serviceInfo.Name} {serviceInfo.ServiceState}");
                InstanciedServiceList.Add(serviceInfo);

                // Notification du serveur
                await ServiceManager.RegisterOrUpdateRunningMicroServiceInfo(new PalaceClient.RunningMicroserviceInfo
                {
                    ServiceName = serviceInfo.Name,
                    StartedDate = DateTime.Now,
                    ServiceState = $"{serviceInfo.ServiceState}",
                    Location = serviceInfo.Location,
                });
            }
        }

    }
}
