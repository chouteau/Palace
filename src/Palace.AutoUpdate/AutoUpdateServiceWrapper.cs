using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate
{
	public class AutoUpdateServiceWrapper : IDisposable
	{
		private string m_TypeName;
		private string m_AssemblyName;
		private string m_FileName;
		private AppDomain m_Domain;
		private object m_Service;

		public AutoUpdateServiceWrapper(string fileName, string qualifiedAssemblyName)
		{
			var typeInfo = qualifiedAssemblyName.Split(',');
			m_TypeName = typeInfo[0].Trim();
			m_AssemblyName = typeInfo[1].Trim();
			m_FileName = fileName;
		}

		public void Initialize()
		{
			System.Diagnostics.Trace.WriteLine(string.Format("Try to start {0} autoupdate service", m_TypeName));
			var setup = AppDomain.CurrentDomain.SetupInformation;
			setup.ShadowCopyFiles = "false";
			m_Domain = AppDomain.CreateDomain(m_TypeName + "AppDomain", null, setup);
			m_Service = m_Domain.CreateInstanceFromAndUnwrap(m_FileName, m_TypeName);

			Invoke("Initialize");

			System.Diagnostics.Trace.WriteLine(string.Format("{0} autoupdate service initialized", m_TypeName));
		}

		public void Start()
		{
			if (m_Service == null)
			{
				return;
			}

			Invoke("Start");
			System.Diagnostics.Trace.WriteLine(string.Format("{0} autoupdate service initialized", m_TypeName));
		}

		public void Stop()
		{
			if (m_Service == null)
			{
				return;
			}

			System.Diagnostics.Trace.WriteLine(string.Format("try to stop {0} service", m_TypeName));

			Invoke("Stop");

			if (m_Domain == null)
			{
				return;
			}

			try
			{
				AppDomain.Unload(m_Domain);
				m_Domain = null;
				GC.Collect();
				System.Diagnostics.Trace.WriteLine("appdomain unload");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.WriteLine(ex.ToString());
			}
			System.Diagnostics.Trace.WriteLine(string.Format("service {0} stopped", m_TypeName));
		}

		public void Dispose()
		{
			m_Domain = null;
			m_Service = null;
		}

		private void Invoke(string methodName)
		{
			var fullAssemblyName = this.GetType().Assembly.Location;
			MethodInvoker methodInvoker = (MethodInvoker)m_Domain.CreateInstanceFromAndUnwrap(fullAssemblyName, "Palace.AutoUpdate.MethodInvoker");
			methodInvoker.Invoke(m_Service, methodName);
		}
	}
}
