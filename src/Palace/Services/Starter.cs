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
            MicroServicesCollectionManager microServicesCollection,
            INotificationService notificationService)
        {
            this.PalaceSettings = palaceSettings;
            this.Logger = logger;
            this.Orchestrator = orchestrator;
            this.MicroServicesCollection = microServicesCollection;
            this.NotificationService = notificationService;
        }

        protected Configuration.PalaceSettings PalaceSettings { get; }
        protected ILogger Logger { get; }
        protected IMicroServicesOrchestrator Orchestrator { get; }
        protected MicroServicesCollectionManager MicroServicesCollection { get; set; }
        protected List<Models.MicroServiceInfo> InstanciedServiceList { get; set; } = new();
        protected INotificationService NotificationService { get; }

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
                        try
                        {
                            await Orchestrator.InstallMicroService(serviceInfo, serviceSettings);
                        }
                        catch(Exception ex)
                        {
                            serviceInfo.ServiceState = Models.ServiceState.InstallationFailed;
                            Logger.LogError(ex, ex.Message);
                        }
                        continue;
                    }

                    break;
                }
            }
        }

        public async Task<bool> GetApplyAction()
        {
            var result = false;
            foreach (var item in MicroServicesCollection.GetList())
            {
				var actionResult = await Orchestrator.GetNextAction(item);
                try
                {
                    await ApplyAction(item, actionResult);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
            }
            return result;
        }

        public async Task<bool> ApplyAction(Models.MicroServiceSettings item, PalaceServer.Models.NextActionResult action)
		{
            bool result = false;
            switch (action?.Action)
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
            return result;
        }
        public async Task<List<(Models.MicroServiceSettings Settings, PalaceServer.Models.NextActionResult NextAction)>> CheckHealth()
        {
            var result = new List<(Models.MicroServiceSettings, PalaceServer.Models.NextActionResult)>();
            foreach (var item in MicroServicesCollection.GetList())
            {
				if (item.MarkHasNew)
				{
                    continue;
				}
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
                    instancied.ServiceState = Models.ServiceState.NotResponding;
                    instancied.NotRespondingCount++;
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{instancied.ServiceState}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(sps);

                    var notRespondingAlert = Math.Min(item.NotRespondingCountBeforeAlert.GetValueOrDefault(int.MaxValue), PalaceSettings.NotRespondingCountBeforeAlert);
					if (instancied.NotRespondingCount > notRespondingAlert)
					{
                        await NotificationService.SendAlert($"Service {item.ServiceName} not responding after {instancied.NotRespondingCount} retries");
					}

                    var notRespondingRestart = Math.Min(item.NotRespondingCountBeforeRestart.GetValueOrDefault(int.MaxValue), PalaceSettings.NotRespondingCountBeforeRestart);
                    if (instancied.NotRespondingCount > notRespondingRestart)
					{
                        result.Add(new(item, new PalaceServer.Models.NextActionResult
                        {
                            Action = PalaceServer.Models.ServiceAction.Stop
                        }));
					}

                    continue;
                }

                instancied.NotRespondingCount = 0;
                instancied.ServiceState = Models.ServiceState.Started;
                instancied.Version = info.Version;
                info.ServiceState = $"{instancied.ServiceState}";
                info.ServiceName = item.ServiceName;
                if (instancied.Process != null)
                {
                    try
					{
                        instancied.Process.Refresh();
						if (!instancied.Process.HasExited)
						{
                            info.PeakWorkingSet = instancied.Process.PeakWorkingSet64;
                            info.PeakVirtualMem = instancied.Process.PeakVirtualMemorySize64;
                            info.PeakPagedMem = instancied.Process.PeakPagedMemorySize64;
                            info.WorkingSet = instancied.Process.WorkingSet64;
                            info.StartedDate = instancied.Process.StartTime;
                            info.ThreadCount = instancied.Process.Threads.Count;
                        }
						else
						{
                            Logger.LogWarning($"process already exited, associated with service {info.ServiceName} reset in progress");
                            instancied.Process = null;
						}
                    }
                    catch(Exception ex)
					{
                        Logger.LogError(ex, ex.Message);
					}
                }
                else if (info.ProcessId != 0)
                {
                    var pi = System.Diagnostics.Process.GetProcessById(info.ProcessId);
                    if (pi != null)
                    {
                        instancied.Process = pi;
                    }
                }

                var threadAlert = Math.Min(item.ThreadLimitBeforeAlert.GetValueOrDefault(int.MaxValue), PalaceSettings.ThreadLimitBeforeAlert);
                if (info.ThreadCount > threadAlert)
				{
                    await NotificationService.SendAlert($"Service {item.ServiceName} thread count greater than {info.ThreadCount}");
                }

				var threadLimit = Math.Min(item.ThreadLimitBeforeRestart.GetValueOrDefault(int.MaxValue), PalaceSettings.ThreadLimitBeforeRestart);
				if (info.ThreadCount > threadLimit)
				{
                    Logger.LogWarning("service {ServiceName} has too many thread {ThreadCount}", info.ServiceName, info.ThreadCount);
                    info.ServiceState = "Instable";
                    result.Add(new(item, new PalaceServer.Models.NextActionResult()
                    {
                        Action = PalaceServer.Models.ServiceAction.Stop
                    }));
                }
                else
				{
                    Logger.LogDebug("service {0} is up", info.ServiceName);
                }

                var maxWorkingSetLimitBeforeAlert = item.MaxWorkingSetLimitBeforeAlert.GetValueOrDefault(long.MaxValue);
                if (info.WorkingSet > maxWorkingSetLimitBeforeAlert)
				{
                    Logger.LogWarning("Warning : service {ServiceName} has working set greater than {maxWorkingSetLimitBeforeAlert}", info.ServiceName, maxWorkingSetLimitBeforeAlert);
                    await NotificationService.SendAlert($"Service {item.ServiceName} has working set greater than {maxWorkingSetLimitBeforeAlert}");
				}

                var maxWorkingSetLimitBeforeRestart = item.MaxWorkingSetLimitBeforeRestart.GetValueOrDefault(long.MaxValue);
                if (info.WorkingSet > maxWorkingSetLimitBeforeRestart)
				{
                    Logger.LogCritical("service {ServiceName} has working set greater than {maxWorkingSetLimitBeforeRestart}", info.ServiceName, maxWorkingSetLimitBeforeRestart);
                    info.ServiceState = "Instable";
                    result.Add(new(item, new PalaceServer.Models.NextActionResult()
                    {
                        Action = PalaceServer.Models.ServiceAction.Stop
                    }));
                }

                await Orchestrator.RegisterOrUpdateRunningMicroServiceInfo(info);
            }
            return result;
        }
        
        public async Task CheckUpdate()
        {
            foreach (var item in MicroServicesCollection.GetList())
            {
                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));
                if (instancied == null
                    && item.AlwaysStarted
                    && !item.MarkHasNew)
                {
                    instancied = await StartMicroService(item);
                }

                if (instancied == null)
                {
                    Logger.LogInformation($"instance of {item.ServiceName} does not exists");
                    instancied = new Models.MicroServiceInfo();
                    instancied.Name = item.ServiceName;
                    instancied.ServiceState = Models.ServiceState.NotInstalled;
                    instancied.Arguments = item.Arguments;
                    instancied.MainFileName = item.MainAssembly;
                    if (item.InstallationFailed)
                    {
                        instancied.ServiceState = Models.ServiceState.InstallationFailed;
                    }
                    else if (!item.AlwaysStarted)
                    {
                        instancied.ServiceState = Models.ServiceState.Offline;
                    }
                    else if (item.MarkHasNew)
					{
                        instancied.ServiceState = Models.ServiceState.InstallationInProgress;
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
                    item.MarkHasNew = false;
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

                if (!instancied.LastWriteTime.HasValue)
				{
                    instancied = await StartMicroService(item);
				}

                var totalMinute = (instancied.LastWriteTime.GetValueOrDefault(DateTime.MinValue) - remoteServiceInfo.LastWriteTime).TotalMinutes;
                if (totalMinute <= 0)
                {
                    Logger.LogInformation("Update detected for service {0}", instancied.Name);
                    var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.UpdateDetected}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(sps);

                    instancied.ServiceState = Models.ServiceState.UpdateInProgress;

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

                if (item.UnInstallationFailed)
                {
                    break;
                }

                var instancied = InstanciedServiceList.SingleOrDefault(i => i.Name.Equals(item.ServiceName, StringComparison.InvariantCultureIgnoreCase));

                Logger.LogInformation("Try to remove service {0}", item.ServiceName);
                await StopMicroService(item);

                try
                {
                    var uninstallResult = await Orchestrator.UninstallMicroService(item);
                    if (!uninstallResult)
                    {
                        Logger.LogCritical("remove service {0} failed", item.ServiceName);
                        item.UnInstallationFailed = true;
                        var spsFail = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.UninstallFailed}");
                        await Orchestrator.UpdateRunningMicroServiceProperty(spsFail);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "remove service {ServiceName} failed", item.ServiceName);
                    item.UnInstallationFailed = true;
                    var spsFail = PalaceServer.Models.ServiceProperties.CreateChangeState(item.ServiceName, $"{Models.ServiceState.UninstallFailed}");
                    await Orchestrator.UpdateRunningMicroServiceProperty(spsFail);
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
                await Task.Delay(5 * 1000);
            }
			else
			{
                Logger.LogWarning("Try to kill service");
                var killSuccess = Orchestrator.KillProcess(serviceSettings);
                if (!killSuccess)
                {
                    Logger.LogCritical("Stop service {0} impossible", serviceSettings.ServiceName);
                    return;
                }
            }

            var sps = PalaceServer.Models.ServiceProperties.CreateChangeState(serviceSettings.ServiceName, $"{Models.ServiceState.Offline}");

            var instantiedService = InstanciedServiceList.SingleOrDefault(i => i.Name == serviceSettings.ServiceName);
            if (instantiedService != null)
            {
                InstanciedServiceList.Remove(instantiedService);
                if (instantiedService.Process != null)
                {
                    var loop = 0;
					while(true)
					{
                        await Task.Delay(1 * 1000);
                        if (instantiedService.Process.HasExited)
						{
                            break;
						}
                        loop++;
                        Logger.LogWarning("stop {0} has process not exited {loop}", serviceSettings.ServiceName, loop);
                        sps = PalaceServer.Models.ServiceProperties.CreateChangeState(serviceSettings.ServiceName, $"{Models.ServiceState.NotExitedAfterStop}");
                        await Task.Delay(4 * 1000);
                        if (loop > 6)
						{
                            break;
						}
					}
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
                serviceInfo = new Models.MicroServiceInfo();
                serviceInfo.Name = serviceSettings.ServiceName;
                serviceInfo.InstallationFolder = serviceSettings.InstallationFolder;
                serviceInfo.LocalInstallationExists = false;
                if (remoteServiceInfo == null)
                {
                    serviceInfo.ServiceState = Models.ServiceState.NotExists;
                    Logger.LogWarning("this service does not exists {0}", serviceSettings.ServiceName);
                }
                else
                {
                    serviceInfo.ServiceState = Models.ServiceState.NotInstalled;
                }
                serviceSettings.MarkHasNew = false;
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
                else
                {
					serviceInfo.ServiceState = Models.ServiceState.StartFail;
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
                instance.InstallationFolder = serviceInfo.InstallationFolder;
                instance.MainFileName = serviceInfo.MainFileName;
                instance.Arguments = serviceInfo.Arguments;
                instance.LocalInstallationExists = serviceInfo.LocalInstallationExists;
            }
        }

    }
}
