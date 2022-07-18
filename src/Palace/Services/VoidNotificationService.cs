using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
	internal class VoidNotificationService : INotificationService
	{
		public Task SendAlert(string message)
		{
			// Do nothing
			return Task.CompletedTask;
		}

		public Task SendNotification(string message)
		{
			// Do nothing
			return Task.CompletedTask;
		}
	}
}
