#define FOR_WINDOWS

using LogRPush;
using Palace;
using Palace.Extensions;

// [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appSettings.json")
                    .Build();

var palaceSection = configuration.GetSection("Palace");
var palaceSettings = new Palace.Configuration.PalaceSettings();
palaceSection.Bind(palaceSettings);
palaceSettings.Initialize();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(palaceSettings);
        services.AddSingleton<Palace.Services.IStarter, Palace.Services.Starter>();

        services.AddTransient<Palace.Services.IMicroServicesOrchestrator, Palace.Services.MicroServicesOrchestrator>();
        services.AddSingleton<Palace.Services.MicroServicesCollectionManager>();

        services.AddLogging(configure =>
        {
            configure.ClearProviders();
            configure.AddFilter("Microsoft", LogLevel.Warning);
            configure.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            configure.AddConsole();
        });
        services.AddMemoryCache();

        services.AddHttpClient();
        var version = typeof(Program).Assembly.GetName().Version.ToString();
        services.AddHttpClient("PalaceServer", configure =>
        {
            configure.BaseAddress = new Uri(palaceSettings.UpdateServerUrl);
            configure.DefaultRequestHeaders.Add("Authorization", $"Basic {palaceSettings.ApiKey}");
            configure.DefaultRequestHeaders.UserAgent.ParseAdd($"Palace/{version} ({System.Environment.OSVersion}; {System.Environment.MachineName}; {palaceSettings.HostName})");
        });

        services.AddLogRPush(cfg =>
        {
            cfg.HostName = palaceSettings.HostName;
            cfg.LogServerUrlList.Add(palaceSettings.UpdateServerUrl);
            cfg.LogLevel = palaceSettings.LogLevel;
        });

        services.AddHostedService<MainService>();
    });

#if FOR_WINDOWS
    builder.UseWindowsService();
#endif

var host = builder.Build();

host.Services.UseLogRPush();

await host.RunAsync();
