using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using PalaceClient;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseKestrel(cfg =>
        {
            var port = 888;
            if (args.Any())
            {
                var nextisport = false;
                foreach (var item in args)
                {
                    if (nextisport)
                    {
                        port = Convert.ToInt32(item);
                        break;
                    }
                    if (item.Equals("--port", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nextisport = true;
                    }
                }
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

StopAwaiter.WaitForStopFromWebApi(host.RunAsync());

await host.StopAsync();

