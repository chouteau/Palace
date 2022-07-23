using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	internal class CleanupBackupBackgroundService : BackgroundService
	{
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			
			await Task.Delay(24 * 60 * 60 * 1000, stoppingToken);
		}
	}
}
