using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Palace.AutoUpdate
{
	public class AutoUpdaterServiceHost
	{
		System.Threading.Thread m_CheckerThread;
		System.Threading.ManualResetEvent m_EventStop;
		bool m_Terminated = false;
		List<AutoUpdateServiceWrapper> m_AutoUpdateServiceList = null;

		public void Initialize()
		{
			m_AutoUpdateServiceList = new List<AutoUpdateServiceWrapper>();
		}

		public void Start()
		{
			m_EventStop = new ManualResetEvent(false);
			m_CheckerThread = new Thread(StartUpdate);
			m_CheckerThread.Name = "PalaceAutoUpdaterThread";
			m_CheckerThread.IsBackground = true;
			m_CheckerThread.Start();
		}

		public void Stop()
		{
			StopAutoUpdateServices();

			m_Terminated = true;
			if(m_EventStop != null)
			{
				m_EventStop.Set();
			}

			if (m_CheckerThread != null
				&& m_CheckerThread.Join(TimeSpan.FromSeconds(1)))
			{
				m_CheckerThread.Abort();
			}
		}

		void StartUpdate()
		{
			RunAutoUpdateServices();

			var interval = new TimeSpan(0, 1, 0);
			while (!m_Terminated)
			{
				GlobalConfiguration.Logger.Debug("Check Updates");

				foreach (var updateUri in GlobalConfiguration.Settings.UpdateUriList)
				{
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
							StopAutoUpdateServices();

							var result = DeployUpdate(updateFileName);
							if (!result)
							{
								continue;
							}

							m_AutoUpdateServiceList.Clear();
							RunAutoUpdateServices();
						}

						interval = new TimeSpan(0, 1, 0);
					}
					catch (System.AggregateException agex)
					{
						var webex = agex.InnerException as System.Net.Http.HttpRequestException;
						if (webex != null)
						{
							GlobalConfiguration.Logger.Warn("UpdateService : " + webex.Message);
							interval = new TimeSpan(0, 5, 0);
						}
						else
						{
							GlobalConfiguration.Logger.Error(agex.InnerException.GetType().FullName);
						}
					}
					catch (System.Net.WebException webex)
					{
						GlobalConfiguration.Logger.Warn(webex.Message);
						interval = new TimeSpan(0, 5, 0);
					}
					catch (UpdateUrlNotAccessibleException)
					{
						GlobalConfiguration.Logger.Warn(string.Format("Uri {0} not accessible", updateUri));
						interval = new TimeSpan(0, 5, 0);
					}
					catch (Exception ex)
					{
						GlobalConfiguration.Logger.Error("UpdateService : \r\n" + ex.ToString());
					}
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

		void RunAutoUpdateServices()
		{
			var list = Starter.GetServiceTypeList("ServiceUpdatableHost");
			if (list == null)
			{
				return;
			}

			foreach (var file in list.Keys)
			{
				foreach (var type in list[file])
				{
					var ausw = new AutoUpdateServiceWrapper(file, type);
					m_AutoUpdateServiceList.Add(ausw);
				}
			}

			GlobalConfiguration.Logger.Info("Try to run autoupdater");
			foreach (var svc in m_AutoUpdateServiceList)
			{
				try
				{
					svc.Initialize();
				}
				catch(Exception ex)
				{
					GlobalConfiguration.Logger.Error(ex.ToString());
				}
			}

			foreach (var svc in m_AutoUpdateServiceList)
			{
				try
				{
					svc.Start();
				}
				catch (Exception ex)
				{
					GlobalConfiguration.Logger.Error(ex.ToString());
				}
			}
		}

		void StopAutoUpdateServices()
		{
			if (m_AutoUpdateServiceList == null)
			{
				return;
			}

			System.Diagnostics.Trace.WriteLine("try to stop and unload running service");

			foreach (var svc in m_AutoUpdateServiceList)
			{
				svc.Stop();
				svc.Dispose();
			}
		}

		bool DeployUpdate(string e)
		{
			GlobalConfiguration.Logger.Info("New update detected");

			// Unzip
			using (var zip = System.IO.Compression.ZipFile.Open(e, System.IO.Compression.ZipArchiveMode.Read))
			{
				foreach (var item in zip.Entries)
				{
					var entry = System.IO.Path.Combine(GlobalConfiguration.CurrentFolder.TrimEnd('\\'), item.FullName);
					var result = DeleteFile(entry);
					if (!result)
					{
						return false;
					}
					GlobalConfiguration.Logger.Info(string.Format("{0} file deleted", entry));
				}
			}

			System.Threading.Thread.Sleep(1 * 1000);
			System.IO.Compression.ZipFile.ExtractToDirectory(e, GlobalConfiguration.CurrentFolder);
			GlobalConfiguration.Logger.Info("unzip new service");

			return true;
		}

		bool DeleteFile(string fileName)
		{
			var deleted = false;
			var retryCount = 0;
			while(true)
			{
				try
				{
					System.IO.File.Delete(fileName);
					deleted = true;
					break;
				}
				catch(Exception ex)
				{
					GlobalConfiguration.Logger.Error(ex.ToString());
					retryCount++;
				}

				if (retryCount > 5)
				{
					break;
				}

				System.Threading.Thread.Sleep(5 * 1000);
			}
			return deleted;
		}

	}
}
