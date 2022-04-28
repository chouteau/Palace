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
			Configuration.PalaceSettings palaceSettings)
		{
			this.Starter = starter;
			this.MicroServicesCollectionManager = microServicesCollectionManager;
			this.PalaceSettings = palaceSettings;
		}

		protected Configuration.PalaceSettings PalaceSettings { get; }
		protected Services.IStarter Starter { get; }
		protected Services.MicroServicesCollectionManager MicroServicesCollectionManager { get; }

		protected System.Timers.Timer Timer { get; }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
			await MicroServicesCollectionManager.SynchronizeConfiguration();
			await Starter.Start();
			await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await MicroServicesCollectionManager.SynchronizeConfiguration();

				var applyAction = await Starter.GetApplyAction();
				if (!applyAction)
				{
					var actionList = await Starter.CheckHealth();
					if (actionList != null
						&& actionList.Count > 0)
					{
						foreach (var item in actionList)
						{
							await Starter.ApplyAction(item.Item1, item.Item2);
						}
					}
					await Starter.CheckUpdate();
					await Starter.CheckRemove();
				}
				
				await Task.Delay(PalaceSettings.ScanIntervalInSeconds * 1000, stoppingToken);
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			await Starter.Stop();
			await base.StopAsync(cancellationToken);
		}
	}
}
