using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Palace
{
	public class Starter
	{
		private List<object> m_InstanciedServiceList;
		private List<Type> m_ServiceList;

		public void Start()
		{
			m_InstanciedServiceList = new List<object>();
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			var list = GetServiceTypeList("ServiceHost");
			watch.Stop();
			GlobalConfiguration.Logger.Info($"{list.Count()} services found in {watch.ElapsedMilliseconds} ms");

			m_ServiceList = new List<Type>();
			foreach (var file in list.Keys)
			{
				foreach (var type in list[file])
				{
					m_ServiceList.Add(Type.GetType(type));
				}
			}

			var failList = new List<Exception>();
			StartServices(failList);
			if (failList.Count > 0)
			{
				var failStartException = new AggregateException("Start services failed", failList);
				throw failStartException;
			}
		}

		public void Stop()
		{
			GlobalConfiguration.Logger.Info("Stop services");

			foreach (var svc in m_InstanciedServiceList)
			{
				var method = svc.GetType().GetMethod("Stop");
				try
				{
					method.Invoke(svc, null);
				}
				catch(Exception ex)
				{
					GlobalConfiguration.Logger.Error(ex.ToString());
				}
			}

			foreach (var svc in m_InstanciedServiceList)
			{
				if (svc is IDisposable)
				{
					try
					{
						((IDisposable)svc).Dispose();
					}
					catch (Exception ex)
					{
						GlobalConfiguration.Logger.Error(ex.ToString());
					}
				}
			}
		}

		void StartServices(List<Exception> failList)
		{
			GlobalConfiguration.Logger.Info(string.Format("Start {0} services", m_ServiceList.Count()));

			var watch = new System.Diagnostics.Stopwatch();

			foreach (var svcType in m_ServiceList)
			{
				System.Diagnostics.Trace.WriteLine(string.Format("Try to start {0} service", svcType.Name));
				try
				{
					watch.Start();
					var svcInstance = Activator.CreateInstance(svcType);
					var initializeMethod = svcType.GetMethod("Initialize");
					initializeMethod.Invoke(svcInstance, null);
					watch.Stop();
					m_InstanciedServiceList.Add(svcInstance);
					GlobalConfiguration.Logger.Info($"Service {svcType.Name} initialized in {watch.ElapsedMilliseconds} ms");
				}
				catch(Exception ex)
				{
					failList.Add(ex);
					GlobalConfiguration.Logger.Error(ex.ToString());
					System.Diagnostics.EventLog.WriteEntry("Application", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
				}
				finally
				{
					watch.Stop();
					watch.Reset();
				}
			}

			GlobalConfiguration.Logger.Info($"-----------------------------------------------------------------------");
			GlobalConfiguration.Logger.Info($"All services initialized");
			GlobalConfiguration.Logger.Info($"-----------------------------------------------------------------------");

			watch.Reset();
			foreach (var svc in m_InstanciedServiceList)
			{
				try
				{
					var initializeMethod = svc.GetType().GetMethod("Start");
					watch.Start();
					initializeMethod.Invoke(svc, null);
					watch.Stop();
					GlobalConfiguration.Logger.Info($"Service {svc.GetType().Name} started in {watch.ElapsedMilliseconds} ms");
				}
				catch (Exception ex)
				{
					failList.Add(ex);
					GlobalConfiguration.Logger.Error(ex.ToString());
					System.Diagnostics.EventLog.WriteEntry("Application", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
				}
				finally
				{
					watch.Stop();
					watch.Reset();
				}
			}

			GlobalConfiguration.Logger.Info($"-----------------------------------------------------------------------");
			GlobalConfiguration.Logger.Info($"All services started");
			GlobalConfiguration.Logger.Info($"-----------------------------------------------------------------------");
		}

		public static Dictionary<string, List<string>> GetServiceTypeList(string suffix)
		{
			var result = new Dictionary<string, List<string>>();
			var fileList = from file in System.IO.Directory.GetFiles(GlobalConfiguration.CurrentFolder, GlobalConfiguration.Settings.SearchPattern, System.IO.SearchOption.AllDirectories)
						   where !System.IO.Path.GetFileName(file).StartsWith("System.",StringComparison.InvariantCultureIgnoreCase)
								&& !System.IO.Path.GetFileName(file).StartsWith("Microsoft.",StringComparison.InvariantCultureIgnoreCase)
						   select file;

			var setup = AppDomain.CurrentDomain.SetupInformation;
			setup.ShadowCopyFiles = "false";
			var domain = AppDomain.CreateDomain("AppDomainAssemblyChecker", null, setup);
			var fullAssemblyName = typeof(Starter).Assembly.Location;
			object o = domain.CreateInstanceFromAndUnwrap(fullAssemblyName, "Palace.AssemblyInspector");

			var inspector = o as AssemblyInspector;

			foreach (var file in fileList)
			{
				try
				{
					var list = inspector.Inspect(file, suffix);
					if (list != null
						&& list.Count() > 0)
					{
						result.Add(file, new List<string>(list));
					}
				}
				catch(Exception ex)
				{
					GlobalConfiguration.Logger.Error(ex.ToString());
				}
			}

			AppDomain.Unload(domain);
			System.Threading.Thread.Sleep(3 * 1000);

			return result;
		}

	}
}
