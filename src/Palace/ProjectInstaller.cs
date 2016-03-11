using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Palace
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : System.Configuration.Install.Installer
	{
		public ProjectInstaller()
		{
			InitializeComponent();
		}

		protected override void OnBeforeInstall(IDictionary savedState)
		{
			savedState["ServiceName"] = GlobalConfiguration.Settings.ServiceName;
			SetServiceName(GlobalConfiguration.Settings.ServiceName);
			base.OnBeforeInstall(savedState);
		}

		protected override void OnAfterInstall(IDictionary savedState)
		{
			base.OnAfterInstall(savedState);
			if (Context != null)
			{
				GlobalConfiguration.SaveSettings();
			}

			var svcName = this.PalaceServiceInstaller.ServiceName;
			var svc = ServiceControllerHelper.GetWindowsService(svcName);
			if (svc != null)
			{
				try
				{
					svc.Start();
				}
				catch(Exception ex)
				{
					System.Diagnostics.Trace.TraceError(ex.ToString());
				}
			}
		}

		protected override void OnBeforeUninstall(IDictionary savedState)
		{
			var svcName = GetStoredServiceName(savedState);
			var svc = ServiceControllerHelper.GetWindowsService(svcName);
			if (svc != null
				&& svc.Status != ServiceControllerStatus.Stopped)
			{
				svc.Stop();
				svc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
			}

			SetServiceName(svcName);
			base.OnBeforeUninstall(savedState);
		}

		private string GetStoredServiceName(IDictionary savedState)
		{
			if (base.Context.Parameters.ContainsKey("ServiceName"))
			{
				return base.Context.Parameters["ServiceName"];
			}
			if (savedState != null
				&& savedState.Contains("ServiceName"))
			{
				return savedState["ServiceName"].ToString();
			}
			return "Palace";
		}

		private void SetServiceName(string serviceName)
		{
			this.PalaceServiceInstaller.ServiceName = serviceName;
			this.PalaceServiceInstaller.DisplayName = GlobalConfiguration.Settings.ServiceDisplayName;
			this.PalaceServiceInstaller.Description = GlobalConfiguration.Settings.ServiceDescription;
		}
	}
}
