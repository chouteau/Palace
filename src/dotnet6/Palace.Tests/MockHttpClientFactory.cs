using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Tests
{
    internal class MockHttpClientFactory : IHttpClientFactory
    {
        public MockHttpClientFactory(IServiceProvider serviceProvider,
            Palace.Configuration.PalaceSettings palaceSettings)
        {
            this.HttpClientFactory = serviceProvider.GetRequiredService<PalaceServerApplication>();
            this.PalaceSettings = palaceSettings;
        }

        protected PalaceServerApplication HttpClientFactory { get; }
        protected Palace.Configuration.PalaceSettings PalaceSettings { get; }

        public HttpClient CreateClient(string name)
        {
            if (name == "PalaceServer")
            {
                var result = HttpClientFactory.CreateClient();
                result.DefaultRequestHeaders.Add("Authorization", $"Basic {PalaceSettings.ApiKey}");
                result.DefaultRequestHeaders.UserAgent.ParseAdd($"Palace/1.0.0.0 ({System.Environment.OSVersion}; {System.Environment.MachineName}; {PalaceSettings.HostName})");
                return result;
            }
            else
            {
                var result = new HttpClient();
                return result;
            }
        }
    }
}
