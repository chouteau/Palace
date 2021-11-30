using Microsoft.Extensions.Caching.Memory;

namespace PalaceServer.Services
{
	public class MicroServiceCollectorManager
	{
		public const string REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY = "availablemicroservicelist:all";
		public const string RUNNING_MICROSERVICE_LIST_CACHE_KEY = "runningmicroservicelist:all";

		public MicroServiceCollectorManager(
			IMemoryCache memoryCache,
			ILogger<MicroServiceCollectorManager> logger,
			Configuration.PalaceServerSettings settings,
			PalaceInfoManager palaceInfoManager
			)
		{
			this.Cache = memoryCache;
			this.Logger = logger;
			this.Settings = settings;
			this.PalaceInfoManager = palaceInfoManager;
		}

		protected IMemoryCache Cache { get; }
		protected ILogger Logger { get; }
		protected Configuration.PalaceServerSettings Settings { get; }
		protected PalaceInfoManager PalaceInfoManager { get; }

		public List<Models.AvailableMicroServiceInfo> GetAvailableList()
		{
			Cache.TryGetValue(REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY, out List<Models.AvailableMicroServiceInfo> result);
			if (result != null)
			{
				return result;
			}
			result = new List<Models.AvailableMicroServiceInfo>();

			var zipFileList = from f in System.IO.Directory.GetFiles(Settings.MicroServiceRepositoryFolder, "*.zip", System.IO.SearchOption.AllDirectories)
					  	      let fileInfo = new System.IO.FileInfo(f)
							  select fileInfo;	
			
			foreach (var item in zipFileList)
			{
				var info = new Models.AvailableMicroServiceInfo
				{
					ServiceName = item.Name.Replace(".zip",""),
					ZipFileName = item.Name,
					LastWriteTime = item.LastWriteTime
				};
				result.Add(info);
			}
			Cache.Set(REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY, result, DateTime.Now.AddDays(1));
			return result;
		}

		public List<Models.ExtendedRunningMicroServiceInfo> GetRunningList()
        {
			Cache.TryGetValue(RUNNING_MICROSERVICE_LIST_CACHE_KEY, out List<Models.ExtendedRunningMicroServiceInfo> result);
			if (result != null)
			{
				return result;
			}
            else
            {
				result = new List<Models.ExtendedRunningMicroServiceInfo>();
            }

			Cache.Set(RUNNING_MICROSERVICE_LIST_CACHE_KEY, result, DateTime.Now.AddDays(1));
			return result;
		}

		public void UpdateFile(string zipFileFullPath)
		{
			var list = GetAvailableList();
			var fi = new System.IO.FileInfo(zipFileFullPath);
			var zipFileName = System.IO.Path.GetFileName(zipFileFullPath);
			var item = list.FirstOrDefault(i => i.ZipFileName.Equals(zipFileName, StringComparison.InvariantCultureIgnoreCase));
			if (item == null)
            {
				var info = new Models.AvailableMicroServiceInfo
				{
					ServiceName = zipFileName.Replace(".zip", ""),
					ZipFileName = zipFileName,
					LastWriteTime = fi.LastWriteTime
				};
				list.Add(info);
			}
            else
            {
				item.LastWriteTime = fi.LastWriteTime;
            }
		}

		public void AddOrUpdateRunningMicroServiceInfo(PalaceClient.RunningMicroserviceInfo runningMicroserviceInfo, string userAgent, string userHostAddress)
        {
			var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(userAgent, userHostAddress);
			if (palaceInfo.HostName == null)
            {
				return;
            }

			var runningList = GetRunningList();

			var key = $"{palaceInfo.MachineName}.{palaceInfo.HostName}.{runningMicroserviceInfo.ServiceName}".ToLower();
			var rms = runningList.SingleOrDefault(i => i.Key == key);
			if (rms == null)
		    {
				rms = new Models.ExtendedRunningMicroServiceInfo();
				rms.ServiceName = runningMicroserviceInfo.ServiceName;
				runningList.Add(rms);
            }
			rms.PalaceInfo = palaceInfo;
			rms.Location = runningMicroserviceInfo.Location;
			rms.UserInteractive = runningMicroserviceInfo.UserInteractive;
			rms.Version = runningMicroserviceInfo.Version;
			rms.LastWriteTime = runningMicroserviceInfo.LastWriteTime;
			rms.ThreadCount = runningMicroserviceInfo.ThreadCount;
			rms.ProcessId = runningMicroserviceInfo.ProcessId;
			rms.LastUpdateDate = DateTime.Now;
			rms.ServiceState = runningMicroserviceInfo.ServiceState;
			rms.StartedDate = runningMicroserviceInfo.StartedDate;
			rms.CommandLine = runningMicroserviceInfo.CommandLine;
			rms.PeakPagedMem = runningMicroserviceInfo.PeakPagedMem;
			rms.PeakVirtualMem = runningMicroserviceInfo.PeakVirtualMem;	
			rms.PeakWorkingSet = runningMicroserviceInfo.PeakWorkingSet;
			if (rms.ServiceState == "Started")
			{
				UnLockDownload(rms.ServiceName);
			}
		}

		public void UpdateRunningMicroServiceProperties(Models.ServiceProperties serviceProperties, string userAgent, string userHostAddress)
		{
			if (serviceProperties == null)
			{ 
				return;
			}
			var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(userAgent, userHostAddress);
			if (palaceInfo.HostName == null)
			{
				return;
			}

			var runningList = GetRunningList();

			var key = $"{palaceInfo.MachineName}.{palaceInfo.HostName}.{serviceProperties.ServiceName}".ToLower();
			var rms = runningList.SingleOrDefault(i => i.Key == key);
			if (rms == null)
			{
				return;
			}

            foreach (var item in serviceProperties.PropertyList)
            {
				var propertyInfo = rms.GetType().GetProperty(item.PropetyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
				if (propertyInfo == null)
                {
					continue;
                }
				var value = System.Convert.ChangeType(item.PropertyValue, propertyInfo.PropertyType);
				propertyInfo.SetValue(rms, value,null);
			}
			rms.LastUpdateDate = DateTime.Now;

			if (rms.ServiceState == "UpdateDetected")
            {
				LockDownload(rms.ServiceName, palaceInfo.Key);
			}
			else if (rms.ServiceState == "Started")
            {
				UnLockDownload(rms.ServiceName);
			}
		}

		private void UnLockDownload(string serviceName)
		{
			var list = GetAvailableList();
			var item = list.FirstOrDefault(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
			if (item != null)
			{
				item.LockedBy = null;
			}
		}

		private void LockDownload(string serviceName, string palaceInfoKey)
        {
			var list = GetAvailableList();
			var item = list.FirstOrDefault(i => i.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
			if (item != null)
            {
				item.LockedBy = palaceInfoKey;
			}
		}

	}
}
