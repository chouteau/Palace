using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace PalaceClient
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddPalaceClient(this IServiceCollection services, Type svc,  Action<PalaceSettings> action)
        {
            var palaceSettings = new PalaceSettings();
            action.Invoke(palaceSettings);
            services.AddSingleton(palaceSettings);

            services.AddMvcCore().AddApplicationPart(typeof(StartupExtensions).Assembly);

            var version = svc.Assembly.GetName().Version.ToString();
            var productAttribute = svc.Assembly.GetCustomAttribute<System.Reflection.AssemblyProductAttribute>();
            var serviceName = productAttribute?.Product;
            var fileInfo = new System.IO.FileInfo(svc.Assembly.Location);

            palaceSettings.Version = version;
            palaceSettings.ServiceName = serviceName;
            palaceSettings.LastWriteTime = fileInfo.LastWriteTime;
            palaceSettings.Location = fileInfo.FullName;

            return services;
        }
    }
}
