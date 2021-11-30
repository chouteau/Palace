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
        services.AddTransient<Starter>();
        services.AddTransient<IMicroServicesManager, MicroServicesManager>();
        services.AddTransient<IAlertNotification, VoidAlertNotification>();

        services.AddLogging();
        services.AddMemoryCache();

        services.AddHttpClient();
        var version = typeof(Program).Assembly.GetName().Version.ToString();
        services.AddHttpClient("PalaceServer", configure =>
        {
            configure.BaseAddress = new Uri(palaceSettings.UpdateServerUrl);
            configure.DefaultRequestHeaders.Add("Authorization", $"Basic {palaceSettings.ApiKey}");
            configure.DefaultRequestHeaders.Add("UserAgent", $"Palace ({System.Environment.OSVersion}; {System.Environment.MachineName}; {palaceSettings.HostName}; {version})");
        });

        services.AddHostedService<MainService>();
    });

#if FOR_WINDOWS
    builder.UseWindowsService();
#endif

var host = builder.Build();

await host.RunAsync();
