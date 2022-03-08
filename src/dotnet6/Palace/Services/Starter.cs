using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public class Starter : IStarter
    {
        private bool _isStarted;

        public Starter(Configuration.PalaceSettings palaceSettings,
            ILogger<Starter> logger,
            IMicroServicesOrchestrator orchestrator,
            IMemoryCache cache,
            MicroServicesCollectionManager microServicesCollection)
        {
            this.PalaceSettings = palaceSettings;
            this.Logger = logger;
            this.Orchestrator = orchestrator;
            this.Cache = cache;
            this.MicroServicesCollection = microServicesCollection;
        }

        protected Configuration.PalaceSettings PalaceSettings { get; }
        protected ILogger Logger { get; }
        protected IMicroServicesOrchestrator Orchestrator { get; }
        protected IMemoryCache Cache { get; }
        protected MicroServicesCollectionManager MicroServicesCollection { get; set; }
        protected List<Models.MicroServiceInfo> InstanciedServiceList { get; set; } = new();

        public int InstanciedServiceCount => InstanciedServiceList.Count;
        public int RunningServiceCount => InstanciedServiceList.Count(i => i.Process != null && !i.Process.HasExited);

        public async Task Start()
        {
            if (_isStarted)
            {
                return;
            }
            _isStarted = true;
            Logger.LogInformation($"Starting {MicroServicesCollection.GetList().Count()} micro services");
            foreach (var serviceSettings in MicroServicesCollection.GetList())
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
                        serviceSettings.InstallationFailed = true;
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
                        Logger.LogInformation("Install this service {0}", serviceInfo.Name);
                        await Orchestrator.InstallMicroService(serviceInfo, serviceSettings);
                        continue;
                    }

                    break;
                }
            }
        }

        public async Task<bool> ApplyAction()
        {
            var result = false;
            foreach (var item in MicroServicesCollection.GetList())
            {
                var actionResult = await Orchestrator.GetNextAction(item);
                switch (actionResult?.Action)
                {
                    case PalaceServer.Models.ServiceAction.Start:
                        Logger.LogInformation("try to start {0}", item.ServiceName);
                        item.StartForced = true;
                        var startResult = await StartMicroService(item);
                        if (startResult == null)
                        {
                            Logger.LogWarning("start {0} fail", item.ServiceName);
                        }
                        result = true;
                        break;

                    case PalaceServer.Models.ServiceAction.Stop:
                        Logger.LogInformation("try to stop {0}", item.ServiceName);
                        item.StartForced = false;
                        await StopMicroService(item);
                        result = true;
                        break;
                    case PalaceServer.Models.ServiceAction.ResetInstallationInfo:
                        Logger.LogInformation("reset installation {0}", item.ServiceName);
                        item.InstallationFailed = false;
                        result = true;
                        break;
                }
            }
            return result;
        }
        public async Task CheckHealth()
        {
            foreach (var item in MicroServicesCollection.GetList())
            {
                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));
                if (instancied == null)
                {
                    continue;
                }

                if (instancied.ServiceState == Models.ServiceState.InstallationFailed)
                {
                    continue;
                }

                if (instancied.ServiceState == Models.ServiceState.Offline
                    && !item.AlwaysStarted 
                    && !item.StartForced)
                {
                    continue;
                }

                var info = await Orchestrator.GetRunningMicroServiceInfo(item);
                if (info == null)
                {
                    var message = $"Service {item.ServiceName} not responding @ {item.AdminServiceUrl}";
                    instancied.ServiceState = Models.ServiceState.NotResponding;
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(sps);
                    continue;
                }
                instancied.ServiceState = Models.ServiceState.Started;
                instancied.Version = info.Version;
                info.ServiceState = $"{instancied.ServiceState}";
                info.ServiceName = item.ServiceName;
                if (instancied.Process != null)
                {
                    info.PeakWorkingSet = instancied.Process.PeakWorkingSet64;
                    info.PeakVirtualMem = instancied.Process.PeakVirtualMemorySize64;
                    info.PeakPagedMem = instancied.Process.PeakPagedMemorySize64;
                    info.StartedDate = instancied.Process.StartTime;
                }
                else if (info.ProcessId != 0)
                {
                    var pi = System.Diagnostics.Process.GetProcessById(info.ProcessId);
                    if (pi != null)
                    {
                        instancied.Process = pi;
                    }
                }
                await Orchestrator.RegisterOrUpdateRunningMicroServiceInfo(info);
                Logger.LogDebug("service {0} is up", info.ServiceName);
            }
        }
        public async Task CheckUpdate()
        {
            foreach (var item in MicroServicesCollection.GetList())
            {
                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));
                if (instancied == null)
                {
                    Logger.LogInformation($"instance of {item.ServiceName} does not exists");
                    instancied = new Models.MicroServiceInfo();
                    instancied.Name = item.ServiceName;
                    instancied.ServiceState = Models.ServiceState.NotInstalled;
                    if (item.InstallationFailed)
                    {
                        instancied.ServiceState = Models.ServiceState.InstallationFailed;
                    }
                    else if (!item.AlwaysStarted)
                    {
                        instancied.ServiceState = Models.ServiceState.Offline;
                    }
                    await AddOrUpdateInstantiatedService(instancied);
                    if ((!item.AlwaysStarted && !item.StartForced)
                        || item.InstallationFailed)
                    {
                        continue;
                    }
                    var result = await Orchestrator.InstallMicroService(instancied, item);
                    if (!result)
                    {
                        instancied.ServiceState = Models.ServiceState.InstallationFailed;
                        item.InstallationFailed = true;
                        var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                        await Orchestrator.UpdateRunningMicroServiceProperty(sps);
                        continue;
                    }
                    Orchestrator.StartMicroService(instancied);
                }

                if (instancied.ServiceState == Models.ServiceState.UpdateInProgress)
                {
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(sps);
                    continue;
                }

                if (instancied.ServiceState == Models.ServiceState.Offline
                    && !item.AlwaysStarted)
                {
                    continue;
                }

                var remoteServiceInfo = await Orchestrator.GetAvailablePackage(item);
                if (remoteServiceInfo == null)
                {
                    Logger.LogWarning("Package removed from server {0}", item.ServiceName);
                    item.InstallationFailed = true;
                    continue;
                }
                var totalMinute = (instancied.LastWriteTime.GetValueOrDefault(DateTime.MinValue) - remoteServiceInfo.LastWriteTime).TotalMinutes;
                if (totalMinute <= 0)
                {
                    Logger.LogInformation("Update detected for service {0}", instancied.Name);
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.UpdateDetected}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(sps);

                    instancied.ServiceState = Models.ServiceState.UpdateInProgress;

                    Orchestrator.BackupMicroServiceFiles(instancied);

                    var update = await Orchestrator.DownloadPackage(item.PackageFileName);
                    if (update != null)
                    {
                        var stopResult = await Orchestrator.StopRunningMicroService(item);
                        if (stopResult == null)
                        {
                            Logger.LogWarning("Stop soft service fail");
                            if (instancied.Process != null)
                            {
                                Logger.LogWarning("Try to kill service");
                                var killSuccess = Orchestrator.KillProcess(instancied);
                                if (!killSuccess)
                                {
                                    Logger.LogCritical("Stop service {0} impossible", item.ServiceName);
                                    continue;
                                }
                            }
                        } 
                        else if (stopResult.Message == "fail")
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
                            await Orchestrator.UpdateRunningMicroServiceProperty(sps);

                            var updateSuccess = await Orchestrator.UpdateMicroService(instancied, update.ZipFileName);
                            if (!updateSuccess)
                            {
                                // Le service est probablement encore en route
                                // On attend le prochain round pour le mettre à jour
                                continue;
                            }

                            sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.Updated}");
                            await Orchestrator.UpdateRunningMicroServiceProperty(sps);

                            Orchestrator.StartMicroService(instancied);
                        }
                    }
                }

            }
        }
        public async Task CheckRemove()
        {
            while(true)
            {
                var item = MicroServicesCollection.GetList().FirstOrDefault(i => i.MarkToDelete);
                if (item == null)
                {
                    break;
                }

                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));

                Logger.LogInformation("Try to remove service {0}", item.ServiceName);
                await StopMicroService(item);

                var uninstallResult = await Orchestrator.UninstallMicroService(item);
                if (!uninstallResult)
                {
                    Logger.LogCritical("remove service {0} failed", item.ServiceName);
                    return;
                }
                MicroServicesCollection.Remove(item);
                var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.Removed}");
                await Orchestrator.UpdateRunningMicroServiceProperty(sps);
                if (instancied != null)
                {
                    InstanciedServiceList.Remove(instancied);
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

            Logger.LogInformation($"Stoping {MicroServicesCollection.GetList().Count()} micro services");
            foreach (var serviceSettings in MicroServicesCollection.GetList())
            {
                await StopMicroService(serviceSettings);
            }
        }

        private async Task StopMicroService(Models.MicroServiceSettings serviceSettings)
        {
            Logger.LogInformation("try to stop {0}", serviceSettings.ServiceName);
            var result = await Orchestrator.StopRunningMicroService(serviceSettings);
            if (result != null)
            {
                Logger.LogInformation("stop {0} with result {1}", serviceSettings.ServiceName, result.Message);
                await Task.Delay(2 * 1000);
            }

            var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(serviceSettings.ServiceName, $"{Models.ServiceState.Offline}");

            var instantiedService = InstanciedServiceList.SingleOrDefault(i => i.Name == serviceSettings.ServiceName);
            if (instantiedService != null)
            {
                InstanciedServiceList.Remove(instantiedService);
                if (instantiedService.Process != null
                    && !instantiedService.Process.HasExited)
                {
                    Logger.LogWarning("stop {0} has process not exited", serviceSettings.ServiceName);
                    sps = PalaceServer.Models.ServiceProperties.CreateChangeState(serviceSettings.ServiceName, $"{Models.ServiceState.NotExitedAfterStop}");
                }
            }

            await Orchestrator.UpdateRunningMicroServiceProperty(sps);
        }

        private async Task<Models.MicroServiceInfo> StartMicroService(Models.MicroServiceSettings serviceSettings)
        {
            // Verification si l'executable est présent
            var serviceInfo = Orchestrator.GetLocallyInstalledMicroServiceInfo(serviceSettings);
            if (serviceInfo == null)
            {
                // S'il 'est pas présent localement, on verifie s'il est présent sur le serveur
                var remoteServiceInfo = await Orchestrator.GetAvailablePackage(serviceSettings);
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
                    serviceInfo.Name = serviceSettings.ServiceName;
                    serviceInfo.ServiceState = Models.ServiceState.NotInstalled;
                }
            }
            else
            {
                serviceInfo.LocalInstallationExists = true;
                // Verification si le service est déjà en ligne
                var runningServiceInfo = await Orchestrator.GetRunningMicroServiceInfo(serviceSettings);
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
                await AddOrUpdateInstantiatedService(serviceInfo);
            }
            else if (serviceInfo.LocalInstallationExists
                && (serviceSettings.AlwaysStarted || serviceSettings.StartForced)
                )
            {
                // Demarrage du service
                Orchestrator.StartMicroService(serviceInfo);
                await Task.Delay(5 * 1000);
                if (serviceInfo.Process == null)
                {
                    Logger.LogWarning("service not started {0}", serviceSettings.ServiceName);
                }
                else if (serviceInfo.ServiceState != Models.ServiceState.StartFail
                    || string.IsNullOrWhiteSpace(serviceInfo.StartFailedMessage))
                {
                    await AddOrUpdateInstantiatedService(serviceInfo);
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(serviceInfo.Name, $"{serviceInfo.ServiceState}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(sps);
                }
            }

            return serviceInfo;
        }

        public Models.MicroServiceInfo GetMicroServiceInfo(string serviceName)
        {
            return InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
        }

        public Models.MicroServiceSettings GetMicroServiceSettings(string serviceName)
        {
            return MicroServicesCollection.GetList().SingleOrDefault(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task AddOrUpdateInstantiatedService(Models.MicroServiceInfo serviceInfo)
        {
            var instance = InstanciedServiceList.SingleOrDefault(i => i.Name == serviceInfo.Name);
            if (instance == null)
            {
                Logger.LogInformation($"micro service {serviceInfo.Name} {serviceInfo.ServiceState}");
                InstanciedServiceList.Add(serviceInfo);

                // Notification du serveur
                await Orchestrator.RegisterOrUpdateRunningMicroServiceInfo(new PalaceClient.RunningMicroserviceInfo
                {
                    ServiceName = serviceInfo.Name,
                    StartedDate = DateTime.Now,
                    ServiceState = $"{serviceInfo.ServiceState}",
                    Location = serviceInfo.InstallationFolder,
                });
            }
            else
            {
                instance.LastWriteTime = serviceInfo.LastWriteTime;
            }
        }

    }
}
