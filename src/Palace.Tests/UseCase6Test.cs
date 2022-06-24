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
    /// Publie sur le serveur le package
    /// 
    /// Check micro service et le demarre
    /// 
    /// Verifie que le service est bien instancié et qu'un process est en route 
    /// 
    /// Demande une suppression du service
    /// 
    /// Verifie que tous les micro services sont supprimés
    /// 
    /// </summary>
    [TestClass]
    public class UseCase6Test
    {
        [TestMethod]
        public async Task Start_And_Remove_Service()
        {
            var host = TestsHelper.CreateTestHostWithServer(config => config.HostName = "Host1");
            TestsHelper.CleanupFolders(host);
            TestsHelper.PublishDemoProject(host);
            var starter = host.Services.GetRequiredService<Palace.Services.IStarter>();

            await starter.Start();
            await starter.CheckHealth();

            starter.InstanciedServiceCount.Should().Be(1);
            starter.RunningServiceCount.Should().Be(1);

            var collection = host.Services.GetRequiredService<Palace.Services.MicroServicesCollectionManager>();
            var list = collection.GetList();
            list.First().MarkToDelete = true;

            await starter.CheckRemove();

            list = collection.GetList();
            list.Count().Should().Be(0);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }
    }
}
