using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Tests
{
    internal class PalaceServerApplication : WebApplicationFactory<Program>
    {
        public PalaceServerApplication(Palace.Configuration.PalaceSettings palaceSettings)
        {
            this.PalaceSettings = palaceSettings;
        }

        protected Palace.Configuration.PalaceSettings PalaceSettings { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var currentDirectory = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            var jsonFile = System.IO.Path.Combine(currentDirectory, "appSettings.json");

            var configuration = new ConfigurationBuilder()
                                        .AddJsonFile(jsonFile)
                                        .Build();

            builder.UseConfiguration(configuration);

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter, MockStartupFilter>();
            });

            base.ConfigureWebHost(builder);
        }
    }
}
