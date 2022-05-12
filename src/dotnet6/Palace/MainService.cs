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
			Services.MicroServicesCollectionManager microServicesCollectionManager,
			Configuration.PalaceSettings palaceSettings,
			ILogger<MainService> logger)
		{
			this.Starter = starter;
			this.MicroServicesCollectionManager = microServicesCollectionManager;
			this.PalaceSettings = palaceSettings;
			this.Logger = logger;
		}

		protected Configuration.PalaceSettings PalaceSettings { get; }
		protected Services.IStarter Starter { get; }
		protected Services.MicroServicesCollectionManager MicroServicesCollectionManager { get; }
		protected ILogger Logger { get; }

		protected System.Timers.Timer Timer { get; }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
			Logger.LogInformation("Service start");
			await MicroServicesCollectionManager.SynchronizeConfiguration(true);
			Logger.LogInformation("Configuraiton synchronized");
			await Starter.Start();
			Logger.LogInformation("Service started");
			await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ExecuteInternalAsync(stoppingToken);
				}
				catch(Exception ex)
				{
					Logger.LogCritical(ex, ex.Message);
				}
			}
		}

		private async Task ExecuteInternalAsync(CancellationToken stoppingToken)
		{
			await MicroServicesCollectionManager.SynchronizeConfiguration(false);

			Logger.LogDebug("GetApplyAction");
			var applyAction = await Starter.GetApplyAction();
			if (!applyAction)
			{
				Logger.LogDebug("CheckHealth");
				var actionList = await Starter.CheckHealth();
				if (actionList != null
					&& actionList.Count > 0)
				{
					foreach (var item in actionList)
					{
						await Starter.ApplyAction(item.Item1, item.Item2);
					}
				}
				Logger.LogDebug("CheckUpdate");
				await Starter.CheckUpdate();
				Logger.LogDebug("CheckRemove");
				await Starter.CheckRemove();
			}

			await Task.Delay(PalaceSettings.ScanIntervalInSeconds * 1000, stoppingToken);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			Logger.LogInformation("Service stop");
			await Starter.Stop();
			await base.StopAsync(cancellationToken);
		}
	}
}
