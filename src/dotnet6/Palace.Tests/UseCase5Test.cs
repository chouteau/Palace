using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Tests
{
    /// <summary>
    /// Démarre 2 hosts differents avec le meme fichier de configuration
    /// 
    /// Publie sur le serveur le package
    /// 
    /// Check micro service et le demarre
    /// 
    /// Verifie que le service est bien instancié et qu'un process est en route sur les 2 hosts
    /// 
    /// Stop le micro services sur les 2 hosts
    /// 
    /// Verifie que tous les micro services sont stopés sur les 2 hosts
    /// 
    /// </summary>
    [TestClass]
    public class UseCase5Test
    {
        [TestMethod]
        public async Task Start_With_2_Hosts()
        {
            var host1 = TestsHelper.CreateTestHostWithServer(config => config.HostName = "Host1");
            TestsHelper.CleanupFolders(host1);
            TestsHelper.PublishDemoProject(host1);
            var starter1 = host1.Services.GetRequiredService<Palace.Services.IStarter>();

            var host2 = TestsHelper.CreateTestHostWithServer(config => config.HostName = "Host2");
            TestsHelper.CleanupFolders(host2);
            TestsHelper.PublishDemoProject(host2);
            var starter2 = host1.Services.GetRequiredService<Palace.Services.IStarter>();

            await starter1.Start();
            await starter2.Start();
            await starter1.CheckHealth();
            await starter2.CheckHealth();

            starter1.InstanciedServiceCount.Should().Be(1);
            starter1.RunningServiceCount.Should().Be(1);
            starter2.InstanciedServiceCount.Should().Be(1);
            starter2.RunningServiceCount.Should().Be(1);

            await starter1.Stop();
            await starter2.Stop();

            starter1.InstanciedServiceCount.Should().Be(0);
            starter2.InstanciedServiceCount.Should().Be(0);
        }
    }
}
