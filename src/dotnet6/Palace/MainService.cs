using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	public class MainService : BackgroundService
	{
		public MainService(Services.IStarter starter, 
			Services.IRemoteConfigurationManager remoteConfigurationManager,
			Configuration.PalaceSettings palaceSettings)
		{
			this.Starter = starter;
			this.RemoteConfigurationManager = remoteConfigurationManager;
			this.PalaceSettings = palaceSettings;
		}

		protected Configuration.PalaceSettings PalaceSettings { get; }
		protected Services.IStarter Starter { get; }
		protected Services.IRemoteConfigurationManager RemoteConfigurationManager { get; }

		protected System.Timers.Timer Timer { get; }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
			await Starter.Start();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var applyAction = await Starter.ApplyAction();
				if (!applyAction)
				{
					await Starter.CheckHealth();
					await Starter.CheckUpdate();
				}
				
				await RemoteConfigurationManager.SynchronizeConfiguration();

				await Task.Delay(PalaceSettings.ScanIntervalInSeconds * 1000, stoppingToken);
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			await Starter.Stop();
		}
	}
}
