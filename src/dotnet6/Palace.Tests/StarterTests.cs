using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Palace.Tests
{
    [TestClass]
    public class StarterTests
    {
        [TestMethod]
        public async Task Start_Without_Server()
        {
            var host = TestsHelper.CreateTestHostWithoutServer();
            TestsHelper.CleanupFolders(host);
            var starter = host.Services.GetRequiredService<IStarter>();

            await starter.Start();

            starter.InstanciedServiceCount.Should().Equals(0);
        }

        [TestMethod]
        public async Task Start_With_Server_Without_AvailableService()
        {
            var host = TestsHelper.CreateTestHostWithServer();
            TestsHelper.CleanupFolders(host);
            var starter = host.Services.GetRequiredService<IStarter>();

            await starter.Start();

            starter.InstanciedServiceCount.Should().Equals(0);
        }

        [TestMethod]
        public async Task Start_With_Server_And_AvailableService()
        {
            var host = TestsHelper.CreateTestHostWithServer();
            TestsHelper.PublishDemoProject(host);
            var starter = host.Services.GetRequiredService<IStarter>();

            await starter.Start();

            starter.InstanciedServiceCount.Should().Be(1);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }
    }
}