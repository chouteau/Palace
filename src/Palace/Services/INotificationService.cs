using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
	public interface INotificationService
	{
		Task SendAlert(string message);
		Task SendNotification(string message);
	}
}
