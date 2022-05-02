using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli
{
	public class ServiceManager
	{
		public bool StopService(string serviceName)
		{
			var serviceController = new ServiceController(serviceName);
			if (serviceController.Status.Equals(ServiceControllerStatus.Stopped)
				|| serviceController.Status.Equals(ServiceControllerStatus.StopPending))
			{
				Console.WriteLine("Service {0} is already stopped or stopping", serviceName);
				return false;
			}

			serviceController.Stop();
			serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
			return true;
		}
	}
}
