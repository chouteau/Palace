using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Palace
{
	public class AutoUpdateStarter
	{
		System.Threading.Thread m_CheckerThread;
		System.Threading.ManualResetEvent m_EventStop;
		bool m_Terminated = false;
		List<AutoUpdateServiceWrapper> m_AutoUpdateServiceList;
		List<string> m_AutoUpdateServiceTypeList;
		private bool m_AutoUpdateInProgress = false;
		private UpdateFileWatcher m_UpdateWatcher;

		public void Start(List<string> list)
		{
			m_AutoUpdateServiceList = new List<AutoUpdateServiceWrapper>();
			m_AutoUpdateServiceTypeList = list;
			m_EventStop = new ManualResetEvent(false);
			m_CheckerThread = new Thread(StartUpdate);
			m_CheckerThread.Name = "UpdaterThread";
			m_CheckerThread.IsBackground = true;
			m_CheckerThread.Start();
		}

		public void Stop()
		{
			StopAutoUpdateServices();

			m_Terminated = true;
			m_EventStop.Set();

			if (m_CheckerThread != null
				&& m_CheckerThread.Join(TimeSpan.FromSeconds(1)))
			{
				m_CheckerThread.Abort();
			}
		}

		void StartUpdate()
		{
			System.Diagnostics.Trace.WriteLine(string.Format("Start {0} updatable services", m_AutoUpdateServiceTypeList.Count()));

			m_UpdateWatcher = new UpdateFileWatcher();
			// m_UpdateWatcher.UpdateAvailable += DeployUpdate;
			// m_UpdateWatcher.Initialize();

			var interval = new TimeSpan(0, 1, 0);
			while (!m_Terminated)
			{
				System.Diagnostics.Debug.WriteLine("Check Updates");

				foreach (var updateUri in GlobalConfiguration.Settings.UpdateUriList)
				{
					var fileName = System.IO.Path.GetFileName(updateUri);
					var zipFile = System.IO.Path.Combine(GlobalConfiguration.GetOrCreateStockDirectory(), fileName);

					Updating.UpdaterBase updater = new Updating.FileUpdater();
					if (updateUri.StartsWith("http"))
					{
						updater = new Updating.HttpUpdater();
					}

					try
					{
						var updateFileName = updater.CheckAndGet(updateUri);
						if (updateFileName != null)
						{
							DeployUpdate(this, updateFileName);
						}

						interval = new TimeSpan(0, 1, 0);
					}
					catch (System.AggregateException agex)
					{
						var webex = agex.InnerException as System.Net.Http.HttpRequestException;
						if (webex != null)
						{
							System.Diagnostics.Trace.TraceWarning("UpdateService : " + webex.Message);
							interval = new TimeSpan(0, 5, 0);
						}
						else
						{
							System.Diagnostics.Trace.TraceError(agex.InnerException.GetType().FullName);
						}
					}
					catch (System.Net.WebException webex)
					{
						System.Diagnostics.Trace.TraceWarning(webex.Message);
						interval = new TimeSpan(0, 5, 0);
					}
					catch (UpdateUrlNotAccessibleException)
					{
						var currentFile = new System.IO.FileInfo(zipFile);
						if (!currentFile.Exists)
						{
							System.Diagnostics.Trace.TraceWarning(string.Format("Uri {0} not accessible", updateUri));
							interval = new TimeSpan(0, 5, 0);
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Trace.TraceError("UpdateService : \r\n" + ex.ToString());
					}
				}

				if (m_AutoUpdateServiceList.Count > 0)
				{
					RunAutoUpdateServices();
				}

				var handles = new WaitHandle[] { m_EventStop };
				int index = WaitHandle.WaitAny(handles, interval, false);
				if (index == 0)
				{
					m_Terminated = true;
					break;
				}
			}
		}

		public void RunAutoUpdateServices()
		{
			System.Diagnostics.Trace.WriteLine("Try to run autoupdater");
			foreach (var assemblyQualifiedName in m_AutoUpdateServiceTypeList)
			{
				var svc = new AutoUpdateServiceWrapper(assemblyQualifiedName);
				svc.Start();
				m_AutoUpdateServiceList.Add(svc);
			}
		}

		public void StopAutoUpdateServices()
		{
			System.Diagnostics.Trace.WriteLine("try to stop and unload running service");
			foreach (var svc in m_AutoUpdateServiceList)
			{
				svc.Stop();
				svc.Dispose();
			}
			m_AutoUpdateServiceList.Clear();
		}

		void DeployUpdate(object sender, string e)
		{
			if (m_AutoUpdateInProgress)
			{
				return;
			}
			m_AutoUpdateInProgress = true;

			System.Diagnostics.Trace.WriteLine("New update detected");
			StopAutoUpdateServices();
			System.Threading.Thread.Sleep(1 * 1000);

			// Unzip
			using (var zip = System.IO.Compression.ZipFile.Open(e, System.IO.Compression.ZipArchiveMode.Read))
			{
				foreach (var item in zip.Entries)
				{
					var entry = System.IO.Path.Combine(GlobalConfiguration.CurrentFolder.TrimEnd('\\'), item.FullName);
					System.IO.File.Delete(entry);
					System.Diagnostics.Trace.WriteLine(string.Format("{0} file deleted", entry));
				}
			}
			System.Threading.Thread.Sleep(1 * 1000);
			System.IO.Compression.ZipFile.ExtractToDirectory(e, GlobalConfiguration.CurrentFolder);
			System.Diagnostics.Trace.WriteLine("unzip new service");

			RunAutoUpdateServices();
			m_AutoUpdateInProgress = false;
		}
	}
}
