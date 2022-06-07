using Microsoft.Extensions.Caching.Memory;

namespace PalaceServer.Services
{
	public class MicroServiceCollectorManager
	{
		public const string REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY = "availablemicroservicelist:all";
		public const string RUNNING_MICROSERVICE_LIST_CACHE_KEY = "runningmicroservicelist:all";

		public event Action OnChanged;

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

		public List<Models.AvailablePackage> GetAvailablePackageList()
		{
			Cache.TryGetValue(REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY, out List<Models.AvailablePackage> result);
			if (result != null)
			{
				return result;
			}
			result = new List<Models.AvailablePackage>();

			var zipFileList = from f in System.IO.Directory.GetFiles(Settings.MicroServiceRepositoryFolder, "*.zip", System.IO.SearchOption.AllDirectories)
					  	      let fileInfo = new System.IO.FileInfo(f)
							  select fileInfo;	
			
			foreach (var item in zipFileList)
			{
				var info = new Models.AvailablePackage
				{
					PackageFileName = item.Name,
					LastWriteTime = item.LastWriteTime,
					Size = item.Length
				};
				SetCurrentVersion(info);
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

		public void BackupAndUpdateRepositoryFile(string zipFileFullPath)
		{
			var zipFileName = System.IO.Path.GetFileName(zipFileFullPath.ToLower());

			// Prise en compte du pattern filename.zip.version.*
			var parts = zipFileName.Split('.').ToList();
			string version = null;
			var index = parts.IndexOf("zip");
			version = string.Join(".", parts.Skip(index + 1).Take(int.MaxValue));
			if (!string.IsNullOrWhiteSpace(version))
			{
				zipFileName = zipFileName.Replace($".{version}", "");
			}

			Logger.LogInformation("BackupAndUpdateRepositoryFile {0} with version {1} zipName {2}", zipFileFullPath, version, zipFileName);
			var list = GetAvailablePackageList();

			var availablePackage = list.FirstOrDefault(i => i.PackageFileName.Equals(zipFileName, StringComparison.InvariantCultureIgnoreCase));
			if (availablePackage != null)
			{
				if (availablePackage.ChangeDetected)
				{
					Logger.LogInformation("BackupAndUpdateRepositoryFile {0} with version {1} change already detected", zipFileFullPath, version);
				}
				availablePackage.ChangeDetected = true;
			}

			Logger.LogInformation("Start BackupAndUpdateRepositoryFile {0} with version {1}", zipFileFullPath, version);

			var destFileName = System.IO.Path.Combine(Settings.MicroServiceRepositoryFolder, zipFileName);
			if (System.IO.File.Exists(destFileName))
			{
				// Backup
				string backupDirectory = Settings.MicroServiceBackupFolder;
				if (string.IsNullOrWhiteSpace(version))
				{
					Logger.LogInformation("Try to BackupAndUpdateRepositoryFile {0}", zipFileFullPath);
					backupDirectory = GetNewBackupDirectory(zipFileName);
					if (!System.IO.Directory.Exists(backupDirectory))
					{
						System.IO.Directory.CreateDirectory(backupDirectory);
					}
					var backupFileName = System.IO.Path.Combine(backupDirectory, zipFileName);
					System.IO.File.Copy(zipFileFullPath, backupFileName, true);
					Logger.LogInformation("Backup from {0} to {1} ", zipFileFullPath, backupFileName);
				}
				else
				{
					Logger.LogInformation("Try to BackupAndUpdateRepositoryFile {0} with version {1}", zipFileFullPath, version);
					var directoryPart = zipFileName.Replace(".zip", "", StringComparison.InvariantCultureIgnoreCase);
					var existingBackupFileName = System.IO.Path.Combine(backupDirectory, directoryPart, version, zipFileName);
					if (System.IO.File.Exists(existingBackupFileName))
					{
						Logger.LogInformation("File {0} with version {1} already backuped without changed", zipFileFullPath, version);
						// Ne pas faire de mise à jour
						return;
					}
					var destDirectory = Path.GetDirectoryName(existingBackupFileName);
					if (!System.IO.Directory.Exists(destDirectory))
					{
						System.IO.Directory.CreateDirectory(destDirectory);
					}
					var backupFileName = System.IO.Path.Combine(destDirectory, zipFileName);
					Logger.LogInformation("Try to Backup from {0} to {1} ", zipFileFullPath, backupFileName);
					System.IO.File.Copy(zipFileFullPath, backupFileName, true);
					Logger.LogInformation("Backup from {0} to {1} ", zipFileFullPath, backupFileName);
				}
			}

			try
			{
				Logger.LogInformation("Try to deploy {0} to {1} ", zipFileFullPath, destFileName);
				System.IO.File.Copy(zipFileFullPath, destFileName, true);
				Logger.LogInformation("package {0} deployed", destFileName);
			}
			catch (IOException ex)
			{
				Logger.LogError(ex, "deploy {0} failed", destFileName);
				return;
			}
			finally
			{
				if (availablePackage != null)
				{
					availablePackage.ChangeDetected = false;
				}
			}

			foreach (var running in GetRunningList())
            {
				running.NextAction = Models.ServiceAction.ResetInstallationInfo;
            }

			OnChanged?.Invoke();

			Cache.Remove(REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY);
		}

		public void AddOrUpdateRunningMicroServiceInfo(PalaceClient.RunningMicroserviceInfo runningMicroserviceInfo, string userAgent, string userHostAddress)
        {
			var palaceInfo = PalaceInfoManager.GetOrCreatePalaceInfo(userAgent, userHostAddress);
			if (palaceInfo.HostName == null)
            {
				return;
            }
			palaceInfo.LastHitDate = DateTime.Now;
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
			rms.WorkingSet = runningMicroserviceInfo.WorkingSet;
			rms.AdminUrl = runningMicroserviceInfo.AdminUrl;
			rms.EnvironmentName = runningMicroserviceInfo.EnvironmentName;
			rms.PalaceClientVersion =	runningMicroserviceInfo.PalaceClientVersion;	
			if (rms.ServiceState == "Started")
			{
				UnLockDownload(rms.ServiceName);
			}
			OnChanged?.Invoke();
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

			palaceInfo.LastHitDate = DateTime.Now;
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

			OnChanged?.Invoke();
		}

		public async Task<string> RemovePackage(string packageFileName)
		{
			var fileName = System.IO.Path.Combine(Settings.MicroServiceRepositoryFolder, packageFileName);
			if (System.IO.File.Exists(fileName))
			{
				try
				{
					System.IO.File.Delete(fileName);
					await Task.Delay(1000);
					Cache.Remove(RUNNING_MICROSERVICE_LIST_CACHE_KEY);
				}
				catch (Exception ex)
				{
					return ex.Message;
				}
			}
			return null;
		}

		public List<FileInfo> GetBackupFileList(string packageFileName)
		{
			var directoryPart = packageFileName.Replace(".zip", "", StringComparison.InvariantCultureIgnoreCase);
			var backupDirectory = System.IO.Path.Combine(Settings.MicroServiceBackupFolder, directoryPart);

			if (!System.IO.Directory.Exists(backupDirectory))
			{
				return new List<FileInfo>();
			}
			var list = from f in System.IO.Directory.GetFiles(backupDirectory, "*.*", SearchOption.AllDirectories)
					   let fileInfo = new FileInfo(f)
					   select fileInfo;

			var result = list.OrderByDescending(i => i.CreationTime).ToList();
			return result;
		}

		public string RollbackPackage(Models.AvailablePackage package, FileInfo fileInfo)
		{
			var destPackage = System.IO.Path.Combine(Settings.MicroServiceRepositoryFolder, package.PackageFileName);
			try
			{
				fileInfo.LastWriteTime = DateTime.Now;
				fileInfo.CreationTime = DateTime.Now;
				System.IO.File.Copy(fileInfo.FullName, destPackage, true);
				Cache.Remove(REPOSITORY_MICROSERVICE_AVAILABLE_LIST_CACHE_KEY);
				OnChanged?.Invoke();
			}
			catch(Exception ex)
			{
				return ex.Message;
			}

			return null;
		}

		private void UnLockDownload(string packageFileName)
		{
			var list = GetAvailablePackageList();
			var item = list.FirstOrDefault(i => i.PackageFileName.Equals(packageFileName, StringComparison.InvariantCultureIgnoreCase));
			if (item != null)
			{
				item.LockedBy = null;
			}
			OnChanged?.Invoke();
		}

		private void LockDownload(string packageFileName, string palaceInfoKey)
        {
			var list = GetAvailablePackageList();
			var item = list.FirstOrDefault(i => i.PackageFileName.Equals(packageFileName, StringComparison.InvariantCultureIgnoreCase));
			if (item != null)
            {
				item.LockedBy = palaceInfoKey;
			}
			OnChanged?.Invoke();
		}

		private string GetNewBackupDirectory(string fileName)
		{
			var version = 1;
			var directoryPart = fileName.Replace(".zip", "", StringComparison.InvariantCultureIgnoreCase);
			string backupDirectory = null;
			while (true)
			{
				backupDirectory = System.IO.Path.Combine(Settings.MicroServiceBackupFolder, directoryPart, $"v{version}");
				if (System.IO.Directory.Exists(backupDirectory))
				{
					version++;
					continue;
				}
				break;
			}
			return backupDirectory;
		}
		private void SetCurrentVersion(Models.AvailablePackage availablePackage)
		{
			if (availablePackage == null)
			{
				return;
			}
			var backupList = GetBackupFileList(availablePackage.PackageFileName);
			if (backupList == null
				|| !backupList.Any())
			{
				availablePackage.CurrentVersion = "unknown";
				return;
			}
			var lastBackup = backupList.First();
			var parts = lastBackup.FullName.Split(@"\");
			var version = parts[parts.Length - 2];
			availablePackage.CurrentVersion = version;
		}
	}
}
