using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using PalaceClient;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseKestrel(cfg =>
        {
            cfg.ListenLocalhost(888);
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

StopAwaiter.Wait(host.RunAsync());

await host.StopAsync();

