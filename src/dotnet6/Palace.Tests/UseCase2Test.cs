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
    /// Démarre sans fichier de configuration des micro services
    /// 
    /// Ajoute un micro service manuellement
    /// 
    /// Publie sur le serveur le package
    /// 
    /// Check le nouveau micro service et le demarre
    /// 
    /// Verifie que le service est bien instancié et qu'un process est en route
    /// 
    /// Stop les micro services
    /// 
    /// Verifie que tous les micro services sont stopés
    /// 
    /// </summary>
    [TestClass]
    public class UseCase2Test
    {
        [TestMethod]
        public async Task Start_With_1_MicroService()
        {
            var host = TestsHelper.CreateTestHostWithServer();
            TestsHelper.CleanupFolders(host);

            var settings = host.Services.GetRequiredService<Palace.Configuration.PalaceSettings>();
            settings.PalaceServicesFileName = null;

            var starter = host.Services.GetRequiredService<Palace.Services.IStarter>();

            await starter.Start();

            starter.InstanciedServiceCount.Should().Be(0);

            await starter.CheckHealth();

            starter.InstanciedServiceCount.Should().Be(0);

            var msm = host.Services.GetRequiredService<Palace.Services.MicroServicesCollectionManager>();
            msm.AddOrUpdate(new Models.MicroServiceSettings
            {
                PackageFileName = "DemoSvc.zip",
                ServiceName = "DemoSvc2",
                MainAssembly = "DemoSvc.dll",
                Arguments = "--port 12346",
                AdminServiceUrl = "http://localhost:12346",
                PalaceApiKey = "test"
            });

            await starter.CheckUpdate();

            starter.InstanciedServiceCount.Should().Be(1);
            starter.RunningServiceCount.Should().Be(0);

            TestsHelper.PublishDemoProject(host);

            await starter.ApplyAction();
            await starter.CheckUpdate();

            starter.InstanciedServiceCount.Should().Be(1);
            starter.RunningServiceCount.Should().Be(1);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }

        [TestMethod]
        public async Task Start_With_2_MicroServices()
        {
            var host = TestsHelper.CreateTestHostWithServer();
            TestsHelper.CleanupFolders(host);

            var settings = host.Services.GetRequiredService<Palace.Configuration.PalaceSettings>();
            settings.PalaceServicesFileName = null;

            var starter = host.Services.GetRequiredService<Palace.Services.IStarter>();

            await starter.Start();

            starter.InstanciedServiceCount.Should().Be(0);

            await starter.CheckHealth();

            starter.InstanciedServiceCount.Should().Be(0);

            var msm = host.Services.GetRequiredService<Palace.Services.MicroServicesCollectionManager>();
            msm.AddOrUpdate(new Models.MicroServiceSettings
            {
                PackageFileName = "DemoSvc.zip",
                ServiceName = "DemoSvc",
                MainAssembly = "DemoSvc.dll",
                Arguments = "--port 12346",
                AdminServiceUrl = "http://localhost:12346",
                PalaceApiKey = "test"
            });

            msm.AddOrUpdate(new Models.MicroServiceSettings
            {
                PackageFileName = "DemoSvc2.zip",
                ServiceName = "DemoSvc2",
                MainAssembly = "DemoSvc.dll",
                Arguments = "--port 12347",
                AdminServiceUrl = "http://localhost:12347",
                PalaceApiKey = "test"
            });

            await starter.CheckUpdate();

            starter.InstanciedServiceCount.Should().Be(2);
            starter.RunningServiceCount.Should().Be(0);

            TestsHelper.PublishDemoProject(host);
            TestsHelper.PublishDemoProject(host, "DemoSvc2.zip");

            await starter.ApplyAction();
            await starter.CheckUpdate();

            starter.InstanciedServiceCount.Should().Be(2);
            starter.RunningServiceCount.Should().Be(2);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }

    }
}
