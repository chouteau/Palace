using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Tests
{
    internal static class TestsHelper
    {
        public static IHost CreateTestHostWithServer()
        {
            var configuration = new ConfigurationBuilder()
                                    .AddJsonFile("appSettings.json")
                                    .Build();

            var palaceSection = configuration.GetSection("Palace");
            var palaceSettings = new Palace.Configuration.PalaceSettings();
            palaceSection.Bind(palaceSettings);
            palaceSettings.Initialize();

            var builder = Host.CreateDefaultBuilder()
                            .ConfigureServices(services =>
                            {
                                services.AddSingleton(palaceSettings);
                                services.AddTransient<IStarter, Starter>();
                                services.AddTransient<IMicroServicesManager, MicroServicesManager>();
                                services.AddTransient<IAlertNotification, Palace.VoidAlertNotification>();

                                services.AddLogging();
                                services.AddMemoryCache();

                                services.AddTransient<PalaceServerApplication>();
                                services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
                            });

            var host = builder.Build();
            return host;
        }

        public static IHost CreateTestHostWithoutServer()
        {
            var configuration = new ConfigurationBuilder()
                                    .AddJsonFile("appSettings.json")
                                    .Build();

            var palaceSection = configuration.GetSection("Palace");
            var palaceSettings = new Palace.Configuration.PalaceSettings();
            palaceSection.Bind(palaceSettings);
            palaceSettings.Initialize();

            var builder = Host.CreateDefaultBuilder()
                            .ConfigureServices(services =>
                            {
                                services.AddSingleton(palaceSettings);
                                services.AddTransient<IStarter, Starter>();
                                services.AddTransient<IMicroServicesManager, MicroServicesManager>();
                                services.AddTransient<IAlertNotification, Palace.VoidAlertNotification>();

                                services.AddLogging();
                                services.AddMemoryCache();

                                services.AddHttpClient();
                                services.AddHttpClient("PalaceServer", configure =>
                                {
                                    configure.DefaultRequestHeaders.Add("Authorization", $"Basic {palaceSettings.ApiKey}");
                                    configure.DefaultRequestHeaders.UserAgent.ParseAdd($"Palace/1.0.0.0 ({System.Environment.OSVersion}; {System.Environment.MachineName}; {palaceSettings.HostName})");
                                });
                            });

            var host = builder.Build();
            return host;
        }


        public static void CleanupFolders(IHost host)
        {
            var config = host.Services.GetRequiredService<IConfiguration>();
            var workingDirectory = config["Test:WorkingDirectory"];
            var deployDirectory = config["PalaceServer:MicroServiceRepositoryFolder"];
            var backupDirectory = config["Palace:BackupDirectory"];
            var updateDirectory = config["Palace:UpdateDirectory"];
            var downloadDirectory = config["Palace:DownloadDirectory"];
            var demoHostDirectory = config["Test:DemoHostDirectory"];

            var currentDirectory = System.IO.Path.GetDirectoryName(typeof(TestsHelper).Assembly.Location);
            var removeDirectoryList = new List<string>();
            workingDirectory = System.IO.Path.Combine(currentDirectory, workingDirectory);
            removeDirectoryList.Add(workingDirectory);
            deployDirectory = System.IO.Path.Combine(currentDirectory, deployDirectory);
            removeDirectoryList.Add(deployDirectory);
            backupDirectory = System.IO.Path.Combine(currentDirectory, backupDirectory);
            removeDirectoryList.Add(backupDirectory);
            updateDirectory = System.IO.Path.Combine(currentDirectory, updateDirectory);
            removeDirectoryList.Add(updateDirectory);
            downloadDirectory = System.IO.Path.Combine(currentDirectory, downloadDirectory);
            removeDirectoryList.Add(downloadDirectory);
            demoHostDirectory = System.IO.Path.Combine(currentDirectory, demoHostDirectory);
            removeDirectoryList.Add(demoHostDirectory);

            foreach (var directory in removeDirectoryList)
            {
                if (!System.IO.Directory.Exists(directory))
                {
                    continue;
                }
                var removeFileList = System.IO.Directory.GetFiles(directory, "*.*", System.IO.SearchOption.AllDirectories);
                foreach (var removeFile in removeFileList)
                {
                    System.IO.File.Delete(removeFile);
                }
                var removeDirectory = System.IO.Directory.GetDirectories(directory, "*.*", System.IO.SearchOption.AllDirectories);
                foreach (var item in removeDirectory)
                {
                    if (!System.IO.Directory.Exists(item))
                    {
                        continue;
                    }
                    System.IO.Directory.Delete(item, true);
                }
            }
        }

        public static void PublishDemoProject(IHost host)
        {
            CleanupFolders(host);

            var config = host.Services.GetRequiredService<IConfiguration>();
            var demoProject = config["Test:DemoProject"];
            var workingDirectory = config["Test:WorkingDirectory"];
            var deployDirectory = config["PalaceServer:MicroServiceRepositoryFolder"];

            var currentDirectory = System.IO.Path.GetDirectoryName(typeof(TestsHelper).Assembly.Location);
            demoProject = System.IO.Path.Combine(currentDirectory, demoProject);
            workingDirectory = System.IO.Path.Combine(currentDirectory, workingDirectory);
            deployDirectory = System.IO.Path.Combine(currentDirectory, deployDirectory);

            var psi = new System.Diagnostics.ProcessStartInfo("dotnet");

            psi.Arguments = $"publish {demoProject}";
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            psi.RedirectStandardOutput = true;

            var process = new Process();
            process.StartInfo = psi;
            process.Start();

            var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            reader.Close();

            Console.WriteLine(output);

            var psiZip = new System.Diagnostics.ProcessStartInfo(@"C:\Program Files\7-Zip\7z.exe");
            psiZip.WorkingDirectory = workingDirectory;
            psiZip.Arguments = @$"a -tzip -r {deployDirectory}\demosvc.zip *";

            psiZip.CreateNoWindow = false;
            psiZip.UseShellExecute = false;
            psiZip.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            psiZip.RedirectStandardOutput = true;

            process = new Process();
            process.StartInfo = psiZip;
            process.Start();

            reader = process.StandardOutput;
            output = reader.ReadToEnd();
            reader.Close();

            Console.WriteLine(output);

        }
    }
}
