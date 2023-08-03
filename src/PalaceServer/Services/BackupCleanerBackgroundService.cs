namespace PalaceServer.Services;

internal class BackupCleanerBackgroundService : BackgroundService
{
	public BackupCleanerBackgroundService(ILogger<BackupCleanerBackgroundService> logger,
		Configuration.PalaceServerSettings settings)
	{
		this.Logger = logger;
		this.ServerSettings = settings;
	}

	protected ILogger Logger { get; }
	protected Configuration.PalaceServerSettings ServerSettings { get; }

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("Start backup cleaner");

		try
		{
			await ExecuteInternal(stoppingToken);
		}
		catch(Exception ex) 
		{
			Logger.LogError(ex, ex.Message);
		}
		await Task.Delay(24 * 60 * 60 * 1000, stoppingToken); // Every days
	}

	private Task ExecuteInternal(CancellationToken stoppingToken)
	{
		var backupDirectoryList = from d in System.IO.Directory.GetDirectories(ServerSettings.MicroServiceBackupFolder, "*.*", SearchOption.TopDirectoryOnly)
								  select d;

		Logger.LogInformation($"Found {backupDirectoryList.Count()} root backup directory");

		foreach (var rootDirectoryPackage in backupDirectoryList)
		{
			var subDirectoryList = (from subd in System.IO.Directory.GetDirectories(rootDirectoryPackage, "*.*", SearchOption.TopDirectoryOnly)
									let directoryInfo = new System.IO.DirectoryInfo(subd)
									orderby directoryInfo.CreationTime descending
									select subd).ToList();

			var retentionCount = ServerSettings.BackupRetentionCount;
			while (true)
			{
				var subd = subDirectoryList.FirstOrDefault();
				if (subd == null)
				{
					break;
				}
				subDirectoryList.Remove(subd);
				if (retentionCount-- > 0)
				{
					Logger.LogInformation($"Keep backup directory {subd}");
					continue;
				}
				Logger.LogInformation($"Remove backup directory {subd}");
				System.IO.Directory.Delete(subd, true);
			}
		}

		return Task.CompletedTask;
	}
}

