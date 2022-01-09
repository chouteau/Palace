using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace PalaceClient
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddPalaceClient(this IServiceCollection services, Action<PalaceSettings> action)
        {
            var palaceSettings = new PalaceSettings();
            action.Invoke(palaceSettings);
            services.AddSingleton(palaceSettings);

            services.AddMvcCore().AddApplicationPart(typeof(StartupExtensions).Assembly);

            var entryAssembly = Assembly.GetEntryAssembly();
            var version = entryAssembly.GetName().Version.ToString();
            var productAttribute = entryAssembly.GetCustomAttribute<System.Reflection.AssemblyProductAttribute>();
            var serviceName = productAttribute?.Product;
            var fileInfo = new System.IO.FileInfo(entryAssembly.Location);

            palaceSettings.PalaceClientVersion = $"{typeof(StartupExtensions).Assembly.GetName().Version}";
            palaceSettings.Version = version;
            palaceSettings.ServiceName = serviceName;
            palaceSettings.LastWriteTime = fileInfo.LastWriteTime;
            palaceSettings.Location = fileInfo.FullName;

            return services;
        }
    }
}
