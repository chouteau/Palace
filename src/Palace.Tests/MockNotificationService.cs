using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Palace.Services;

namespace Palace.Tests
{
	internal class MockNotificationService : INotificationService
	{
		public Task SendAlert(string message)
		{
			return Task.CompletedTask;
		}

		public Task SendNotification(string message)
		{
			return Task.CompletedTask;
		}
	}
}
