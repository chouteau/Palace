using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Palace
{
	public class Starter
	{
		private List<object> m_InstanciedServiceList;
		private List<Type> m_ServiceList;
		private AutoUpdateStarter m_AutoUpdateStarter;

		public void Start()
		{
			m_AutoUpdateStarter = new AutoUpdateStarter();
			m_InstanciedServiceList = new List<object>();
			var list = GetServiceTypeList();
			System.Diagnostics.Trace.WriteLine(string.Format("{0} services found", list.Count()));
			m_ServiceList = list.Where(i => !i.Item1).Select(i => Type.GetType(i.Item2)).ToList();
			var autoUpdateServiceHostList = list.Where(i => i.Item1).Select(i => i.Item2).ToList();

			StartServices();
			m_AutoUpdateStarter.Start(autoUpdateServiceHostList);

			GlobalConfiguration.SaveSettings();
		}

		public void Stop()
		{
			m_AutoUpdateStarter.Stop();
			System.Diagnostics.Trace.WriteLine("Stop services");

			foreach (var svc in m_InstanciedServiceList)
			{
				var method = svc.GetType().GetMethod("Stop");
				try
				{
					method.Invoke(svc, null);
				}
				catch(Exception ex)
				{
					System.Diagnostics.Trace.TraceError(ex.ToString());
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
						System.Diagnostics.Trace.TraceError(ex.ToString());
					}
				}
			}
		}

		void StartServices()
		{
			System.Diagnostics.Trace.WriteLine(string.Format("Start {0} services", m_ServiceList.Count()));

			foreach (var svcType in m_ServiceList)
			{
				System.Diagnostics.Trace.WriteLine(string.Format("Try to start {0} service", svcType.Name));
				var svcInstance = Activator.CreateInstance(svcType);
				var method = svcType.GetMethod("Start");
				method.Invoke(svcInstance, null);
				m_InstanciedServiceList.Add(svcInstance);
				System.Diagnostics.Trace.WriteLine(string.Format("Service {0} started", svcType.Name));
			}
		}

		IEnumerable<Tuple<bool, string>> GetServiceTypeList()
		{
			var result = new List<Tuple<bool, string>>();
			var fileList = System.IO.Directory.GetFiles(GlobalConfiguration.CurrentFolder, "*.dll", System.IO.SearchOption.AllDirectories).ToList();

			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (s, arg) =>
			{
				Assembly refType = null;
				try
				{
					refType = Assembly.ReflectionOnlyLoad(arg.Name);
				}
				catch
				{
					return arg.RequestingAssembly;
				}
				return refType;
			};

			foreach (var file in fileList)
			{
				try
				{
					var list = GetServicesInfo(file);
					if (list != null)
					{
						result.AddRange(list);
					}
				}
				catch(Exception ex)
				{
					System.Diagnostics.Trace.TraceError(ex.ToString());
				}
			}

			return result;
		}

		private IEnumerable<Tuple<bool, string>> GetServicesInfo(string file)
		{
			var setup = AppDomain.CurrentDomain.SetupInformation;
			setup.ShadowCopyFiles = "true";
			setup.ApplicationBase = GlobalConfiguration.CurrentFolder;
			setup.PrivateBinPath = GlobalConfiguration.GetOrCreateInspectDirectory();
			setup.LoaderOptimization = LoaderOptimization.MultiDomainHost;
			var domain = AppDomain.CreateDomain("AppDomainAssemblyChecker", null, setup);
			domain.AssemblyResolve += (s, arg) =>
			{
				Assembly refType = null;
				try
				{
					refType = Assembly.ReflectionOnlyLoad(arg.Name);
				}
				catch
				{
					return arg.RequestingAssembly;
				}
				return refType;
			};

			var bytes = System.IO.File.ReadAllBytes(file);
			var assembly = domain.Load(bytes);
			var typelist = (from type in assembly.GetExportedTypes()
						   let methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
						   let autoUpdateService = type.Name.EndsWith("AutoUpdateServiceHost")
						   where type.Name.EndsWith("ServiceHost")
							   && methods.Select(i => i.Name).Contains("Start")
							   && methods.Select(i => i.Name).Contains("Stop")
						   select new Tuple<bool, string>(autoUpdateService, type.AssemblyQualifiedName)).ToList();

			AppDomain.Unload(domain);

			return typelist;
		}

	}
}
