using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	public class MainService : BackgroundService
	{
		private bool _running = false;

		public MainService(IStarter starter, IServiceProvider serviceProvider,
			Configuration.PalaceSettings palaceSettings)
		{
			this.ServiceProvider = serviceProvider;
			this.PalaceSettings = palaceSettings;
		}

		protected Configuration.PalaceSettings PalaceSettings { get; }
		protected IServiceProvider ServiceProvider { get; }
		protected System.Timers.Timer Timer { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var starter = ServiceProvider.GetRequiredService<IStarter>();
				if (!_running)
				{
					await starter.Start();
					_running = true;
				}
				else
				{
					var applyAction = await starter.ApplyAction();
					if (!applyAction)
					{
						await starter.CheckHealth();
						await starter.CheckUpdate();
					}
				}
				await Task.Delay(PalaceSettings.ScanIntervalInSeconds * 1000, stoppingToken);
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			var starter = ServiceProvider.GetRequiredService<IStarter>();
			if (_running)
			{
				await starter.Stop();
			}
		}
	}
}
