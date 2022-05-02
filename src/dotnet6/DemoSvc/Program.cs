using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using PalaceClient;
using DemoSvc;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseKestrel(cfg =>
        {
            var port = 888;
            var portCommandLine = args.GetParameterValue("port");
            if (!string.IsNullOrWhiteSpace(portCommandLine))
            {
                port = Convert.ToInt32(portCommandLine);
            }
            cfg.ListenLocalhost(port);
        });
        webBuilder.Configure((ctx, app) =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        });
    })
    .ConfigureServices(services =>
    {
        services.AddControllers();

        services.AddHostedService<DemoSvc.Worker>();

        services.AddPalaceClient(config =>
        {
            config.ApiKey = "test";
        });
    })
    .Build();

await host.StartAsync();

if (!string.IsNullOrWhiteSpace(System.Environment.GetCommandLineArgs().GetParameterValue("crash")))
{
    throw new Exception();
}

StopAwaiter.WaitForStopFromWebApi();

await Task.Delay(40 * 1000);

await host.StopAsync();
