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
    /// Ajoute un micro service manuellement qui doit crasher au demarrage
    /// 
    /// Publie sur le serveur le package
    /// 
    /// Check le nouveau micro service et le demarre
    /// 
    /// Verifie que le service n'est pas instancié et qu'aucun process n'est en route
    /// 
    /// Stop les micro services
    /// 
    /// </summary>
    [TestClass]
    public class UseCase3Test
    {
        [TestMethod]
        public async Task Start_With_Crashed_MicroService()
        {
            var host = TestsHelper.CreateTestHostWithServer();
            TestsHelper.CleanupFolders(host);

            var settings = host.Services.GetRequiredService<Palace.Configuration.PalaceSettings>();

            var starter = host.Services.GetRequiredService<Palace.Services.IStarter>();

            var msm = host.Services.GetRequiredService<Palace.Services.MicroServicesCollectionManager>();
            await msm.SynchronizeConfiguration();

            //msm.AddOrUpdate(new Models.MicroServiceSettings
            //{
            //    PackageFileName = "DemoSvc.zip",
            //    ServiceName = "DemoSvc3",
            //    MainAssembly = "DemoSvc.dll",
            //    Arguments = "--port 12348 --crash true",
            //    AdminServiceUrl = "http://localhost:12348",
            //    PalaceApiKey = "test"
            //});

            TestsHelper.PublishDemoProject(host);

            await starter.Start();

            await starter.CheckHealth();

            var svc = starter.GetMicroServiceInfo("DemoSvc4");
            svc.Should().BeNull();

            var svcSettings = starter.GetMicroServiceSettings("DemoSvc4");
            svcSettings.Should().NotBeNull();

            starter.InstanciedServiceCount.Should().Be(0);
            starter.RunningServiceCount.Should().Be(0);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }
    }
}
