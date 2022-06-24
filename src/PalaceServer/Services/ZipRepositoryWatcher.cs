using Microsoft.Extensions.Caching.Memory;

namespace PalaceServer.Services;

public class ZipRepositoryWatcher : BackgroundService
{
    public ZipRepositoryWatcher(Configuration.PalaceServerSettings settings,
        MicroServiceCollectorManager microServiceCollectorManager,
        ILogger<ZipRepositoryWatcher> logger)
    {
        this.Settings = settings;
        this.MicroServiceCollectorManager = microServiceCollectorManager;
        this.Logger = logger;
    }

    protected FileSystemWatcher Watcher { get; set; }
    protected Configuration.PalaceServerSettings Settings { get; }
    protected MicroServiceCollectorManager MicroServiceCollectorManager { get; }
    protected ILogger Logger { get; }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!System.IO.Directory.Exists( Settings.MicroServiceStagingFolder))
        {
            return base.StartAsync(cancellationToken); 
        }
        Watcher = new FileSystemWatcher(Settings.MicroServiceStagingFolder);
        Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
        Watcher.Changed += OnChanged;
        Watcher.Created += OnChanged;
        Watcher.EnableRaisingEvents = true;

        return base.StartAsync(cancellationToken);
    }

    private async void OnChanged(object sender, FileSystemEventArgs args)
    {
		Logger.LogDebug("Detect file {ChangeType} {FullPath}", args.FullPath, args.ChangeType);
		
		if (args.Name.EndsWith(".tmp", StringComparison.InvariantCultureIgnoreCase))
		{
            Logger.LogTrace("detect temp file {0} {1}", args.Name, args.ChangeType);
            return;
		}

        // Filtrer sur les zip uniquement
        if (args.Name.IndexOf(".zip", StringComparison.InvariantCultureIgnoreCase) == -1)
        {
            Logger.LogTrace("detect {0} {1} not zip file", args.Name, args.ChangeType);
            return;
        }

        var unlock = await WaitForUnlock(args.FullPath);
        if (!unlock)
        {
            Logger.LogWarning("detect {0} file {1} not closed", args.Name, args.ChangeType);
            return;
        }

        Logger.LogInformation("File unlocked detected {ChangeType} {FullPath}", args.FullPath, args.ChangeType);

        try
        {
            MicroServiceCollectorManager.BackupAndUpdateRepositoryFile(args.FullPath);
        }
        catch (Exception ex)
		{
            ex.Data.Add("FullPath", args.FullPath);
            Logger.LogError(ex, ex.ToString());
		}
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = false;
            Watcher.Changed -= OnChanged;
            Watcher.Created -= OnChanged;
            Watcher.Deleted -= OnChanged;
        }

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        Watcher?.Dispose();
        base.Dispose();
    }

    public async Task<bool> WaitForUnlock(string fileName)
    {
        var loop = 0;
        bool success = false;
        while(true)
		{
            try
            {
                using var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                if (stream.Length > 0)
				{
                    success = true;
                    break;
                }
            }
            catch (IOException)
            {
				Logger.LogDebug("Wait for unlock {0}", fileName);
				await Task.Delay(500);
            }
            catch (Exception ex)
			{
                ex.Data.Add("FileName", fileName);
                Logger.LogError(ex, ex.Message);
                await Task.Delay(500);
            }
            loop++;
            if (loop > 1000)
			{
                success = false;
                break;
			}
        }
        return success;
    }
}

