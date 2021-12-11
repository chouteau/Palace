#define FOR_WINDOWS

using Palace;

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
        services.AddTransient<IStarter, Starter>();
        services.AddTransient<IMicroServicesManager, MicroServicesManager>();
        services.AddTransient<IAlertNotification, VoidAlertNotification>();

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

        services.AddHostedService<MainService>();
    });

#if FOR_WINDOWS
    builder.UseWindowsService();
#endif

var host = builder.Build();

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
loggerFactory.AddProvider(new Palace.Logging.PalaceLoggerProvider(host.Services));

await host.RunAsync();
