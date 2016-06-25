using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate
{
	[Serializable]
	public class AutoUpdateServiceWrapper : IDisposable
	{
		private string m_TypeName;
		private string m_AssemblyName;
		private AppDomain m_Domain;
		private object m_Service;

		public AutoUpdateServiceWrapper(string qualifiedAssemblyName)
		{
			var typeInfo = qualifiedAssemblyName.Split(',');
			m_TypeName = typeInfo[0].Trim();
			m_AssemblyName = typeInfo[1].Trim();
		}

		public void Initialize()
		{
			System.Diagnostics.Trace.WriteLine(string.Format("Try to start {0} autoupdate service", m_TypeName));
			var setup = AppDomain.CurrentDomain.SetupInformation;
			setup.ShadowCopyFiles = "true";
			setup.ApplicationBase = GlobalConfiguration.CurrentFolder;
			setup.PrivateBinPath = GlobalConfiguration.GetOrCreateInspectDirectory();
			setup.LoaderOptimization = LoaderOptimization.MultiDomainHost;
			m_Domain = AppDomain.CreateDomain(m_TypeName + "AppDomain", null, setup);
			m_Service = m_Domain.CreateInstanceAndUnwrap(m_AssemblyName, m_TypeName);
			var initializeMethod = m_Service.GetType().GetMethod("Initialize");
			initializeMethod.Invoke(m_Service, null);
			System.Diagnostics.Trace.WriteLine(string.Format("{0} autoupdate service initialized", m_TypeName));
		}

		public void Start()
		{
			if (m_Service == null)
			{
				return;
			}
			var startMethod = m_Service.GetType().GetMethod("Start");
			startMethod.Invoke(m_Service, null);
			System.Diagnostics.Trace.WriteLine(string.Format("{0} autoupdate service initialized", m_TypeName));
		}

		public void Stop()
		{
			System.Diagnostics.Trace.WriteLine(string.Format("try to stop {0} service", m_TypeName));
			if (m_Service != null)
			{
				try
				{
					var methodInfo = m_Service.GetType().GetMethod("Stop");
					methodInfo.Invoke(m_Service, null);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Trace.WriteLine(ex.ToString());
				}
			}

			if (m_Domain != null)
			{
				try
				{
					AppDomain.Unload(m_Domain);
					m_Domain = null;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Trace.WriteLine(ex.ToString());
				}
				System.Diagnostics.Trace.WriteLine(string.Format("service {0} stopped", m_TypeName));
			}
		}

		public void Dispose()
		{
			m_Domain = null;
			m_Service = null;
		}
	}
}
