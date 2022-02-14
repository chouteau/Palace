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

        if (!IsFileClosed(args.FullPath))
        {
            return;
        }

        var zipFileName = args.FullPath;

        // Prise en compte du patter filename.zip.version
        var parts = args.Name.Split('.');
        string version = null;
        if (!parts.Last().Equals("zip", StringComparison.InvariantCultureIgnoreCase))
		{
            version = parts.Last();
            zipFileName = args.FullPath.Replace($".{version}", "");
        }

        MicroServiceCollectorManager.BackupAndUpdateRepositoryFile(zipFileName, version);
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

    public bool IsFileClosed(string fileName)
    {
        try
        {
            using var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch(IOException)
        {
            return false;
        }
    }
}

