using Microsoft.Extensions.Caching.Memory;

namespace PalaceServer.Services;

public class ZipRepositoryWatcher : BackgroundService
{
    private FileSystemWatcher _watcher;
    private readonly Configuration.PalaceServerSettings _settings;
    private readonly MicroServiceCollectorManager _microServiceCollectorManager;

    public ZipRepositoryWatcher(Configuration.PalaceServerSettings settings,
        MicroServiceCollectorManager microServiceCollectorManager)
    {
        this._settings = settings;
        this._microServiceCollectorManager = microServiceCollectorManager;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!System.IO.Directory.Exists( _settings.MicroServiceRepositoryFolder))
        {
            return base.StartAsync(cancellationToken); 
        }
        _watcher = new FileSystemWatcher(_settings.MicroServiceRepositoryFolder);
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.EnableRaisingEvents = true;

        return base.StartAsync(cancellationToken);
    }

    private async void OnChanged(object sender, FileSystemEventArgs args)
    {
        // Filtrer sur les zip uniquement
        if (!args.Name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }
        if (await IsFileLocked(args.FullPath))
        {
            _microServiceCollectorManager.UpdateFile(args.FullPath);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnChanged;
            _watcher.Created -= OnChanged;
            _watcher.Deleted -= OnChanged;
        }

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }

    public async Task<bool> IsFileLocked(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }
        if (!System.IO.File.Exists(fileName))
        {
            return true;
        }
        try
        {
            await Task.Delay(500);
            using var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}

