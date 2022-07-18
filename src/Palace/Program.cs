#define WINDOWS

using LogRPush;
using Palace;
using Palace.Extensions;

// [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var builder = Host.CreateDefaultBuilder(args)
#if WINDOWS
    .UseWindowsService()
#endif
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
        config
            .SetBasePath(currentDirectory)
            .AddJsonFile("appSettings.json")
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);

        hostingContext.HostingEnvironment.ApplicationName = "Palace";
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.ClearProviders();
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        if (System.Environment.UserInteractive)
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddConsole();
            logging.AddDebug();
        }
    })
    .ConfigureServices((hostingContext, services) =>
    {
        var palaceSection = hostingContext.Configuration.GetSection("Palace");
        var palaceSettings = new Palace.Configuration.PalaceSettings();
        palaceSection.Bind(palaceSettings);
        palaceSettings.Initialize();
        services.AddSingleton(palaceSettings);

        var smtpSection = hostingContext.Configuration.GetSection("SmtpSettings");
        var smtpSettings = new Palace.Configuration.SmtpSettings();
		smtpSection.Bind(smtpSettings);
        services.AddSingleton(smtpSettings);

        services.AddSingleton<Palace.Services.IStarter, Palace.Services.Starter>();

        services.AddTransient<Palace.Services.IMicroServicesOrchestrator, Palace.Services.MicroServicesOrchestrator>();
        services.AddSingleton<Palace.Services.MicroServicesCollectionManager>();
        services.AddTransient<Palace.Services.INotificationService, Palace.Services.SmtpNotificationService>();

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
            cfg.EnvironmentName = hostingContext.HostingEnvironment.EnvironmentName;
		});

		services.AddHostedService<MainService>();
    });

var host = builder.Build();

host.Services.UseLogRPush();

await host.RunAsync();
