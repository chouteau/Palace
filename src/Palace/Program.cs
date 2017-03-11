using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

			if (System.Environment.UserInteractive)
			{
				StartConsole(args);
			}
			else
			{
				var ServicesToRun = new ServiceBase[] 
				{ 
					new MainService() 
				};
				ServiceBase.Run(ServicesToRun);
			}
		}

		private static void StartConsole(string[] args)
		{
			string parameter = string.Concat(args);
			switch (parameter)
			{
				case "/install":
					ServiceControllerHelper.InstallService(GlobalConfiguration.Settings.ServiceName);
					break;
				case "/uninstall":
					ServiceControllerHelper.UninstallService(GlobalConfiguration.Settings.ServiceName);
					break;
				default:
					var starter = new Starter();
					starter.Start();

					GlobalConfiguration.Logger.Info($"{GlobalConfiguration.Settings.ServiceName} started in console mode");

					System.Console.ReadKey();

					starter.Stop();
					break;
			}
		}

		private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var folder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var errorFile = System.IO.Path.Combine(folder, "error.txt");
			var ex = (Exception)e.ExceptionObject;
			var row = string.Format("{0}{1}{2}", ex.Message, System.Environment.NewLine, ex.ToString());
			File.AppendAllText(errorFile, row);
		}

	}
}
