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
    /// Demarre sans configuration de micro service
    /// 
    /// Ajoute un service manuellement
    /// 
    /// Verifie que celui-ci n'étant pas déployé n'est pas démarré
    /// 
    /// </summary>
    [TestClass]
    public class UseCase1Test
    {
        [TestMethod]
        public async Task Start()
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
            await msm.SynchronizeConfiguration();

            //msm.AddOrUpdate(new Models.MicroServiceSettings
            //{
            //    PackageFileName = "DemoSvc.zip",
            //    ServiceName = "DemoSvc",
            //    MainAssembly = "DemoSvc.dll",
            //    Arguments = "--port 12346",
            //    AdminServiceUrl = "http://localhost:12346",
            //    PalaceApiKey = "test"
            //});

            await starter.CheckUpdate();

            starter.InstanciedServiceCount.Should().Be(1);
            starter.RunningServiceCount.Should().Be(0);

            await starter.Stop();

            starter.InstanciedServiceCount.Should().Be(0);
        }

    }
}
