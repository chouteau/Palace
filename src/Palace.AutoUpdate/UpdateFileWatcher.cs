using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate
{
	public class UpdateFileWatcher
	{
		private FileSystemWatcher m_Fsw;

		public UpdateFileWatcher()
		{
		}

		public event EventHandler<string> UpdateAvailable;

		public void Initialize()
		{
			var directory = new System.IO.DirectoryInfo(GlobalConfiguration.GetOrCreateStockDirectory());
			m_Fsw = new FileSystemWatcher();
			m_Fsw.Path = directory.FullName;
			m_Fsw.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
			m_Fsw.Filter = "*.zip";
			m_Fsw.IncludeSubdirectories = false;
			m_Fsw.Changed += Fsw_Changed;
			m_Fsw.Created += Fsw_Changed;
			m_Fsw.Renamed += Fsw_Changed;
			m_Fsw.EnableRaisingEvents = true;
		}

		private void Fsw_Changed(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Deleted)
			{
				return;
			}
			var fileName = System.IO.Path.GetFileName(e.FullPath);
			var destFileName = System.IO.Path.Combine(GlobalConfiguration.CurrentFolder, fileName);
			System.Diagnostics.Trace.WriteLine(string.Format("Copy {0} to {1}", e.FullPath, destFileName));
			try
			{
				System.IO.File.Copy(e.FullPath, destFileName, true);
				if (UpdateAvailable != null)
				{
					UpdateAvailable(this, destFileName);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.TraceError(ex.Message);
			}
		}
	}
}
