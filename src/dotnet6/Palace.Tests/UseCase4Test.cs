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
    /// Publie une mise à jour du service avec son package
    /// 
    /// Check la mise à jour du micro service et le redemarre
    /// 
    /// Verifie que le service est bien instancié et qu'un process est en route
    /// 
    /// Stop les micro services
    /// 
    /// Verifie que tous les micro services sont stopés
    /// 
    /// </summary>
    [TestClass]
    public class UseCase4Test
    {
        [TestMethod]
        public async Task Start_With_Crashed_MicroService()
        {
            var host = TestsHelper.CreateTestHostWithServer();
            TestsHelper.CleanupFolders(host);

            var settings = host.Services.GetRequiredService<Palace.Configuration.PalaceSettings>();
            settings.PalaceServicesFileName = null;

            var starter = host.Services.GetRequiredService<Palace.Services.IStarter>();

            var msm = host.Services.GetRequiredService<Palace.Services.MicroServicesCollectionManager>();
            msm.Add(new Models.MicroServiceSettings
            {
                PackageFileName = "DemoSvc.zip",
                ServiceName = "DemoSvc4",
                MainAssembly = "DemoSvc.dll",
                Arguments = "--port 12349",
                AdminServiceUrl = "http://localhost:12349",
                PalaceApiKey = "test"
            });

            TestsHelper.PublishDemoProject(host);

            await starter.Start();
            await starter.CheckHealth();

            var svc = starter.GetMicroServiceInfo("DemoSvc4");
            svc.Should().NotBeNull();
            var version = svc.Version;

            starter.InstanciedServiceCount.Should().Be(1);
            starter.RunningServiceCount.Should().Be(1);

            // Mise à jour du service
            TestsHelper.UpdateVersionDemoProject(host);
            TestsHelper.PublishDemoProject(host);

            // Temps de mise à jour du package LastWriteTime
            await Task.Delay(12 * 1000);

            await starter.CheckHealth();
            await starter.CheckUpdate();
            await starter.CheckHealth();

            svc = starter.GetMicroServiceInfo("DemoSvc4");
            var newVersion = svc.Version;

            version.Should().NotBe(newVersion);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }
    }
}
