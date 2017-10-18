using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate.Updating
{
	public class FileUpdater : UpdaterBase
	{
		public override string CheckAndGet(string updateUri)
		{
			var stockFileName = System.IO.Path.GetFileName(updateUri);
			var stockFileInfo = new System.IO.FileInfo(updateUri);
			var inspectFileName = System.IO.Path.Combine(DirectoryHelpers.GetOrCreateInspectDirectory(), stockFileName);
			var inspectFileInfo = new System.IO.FileInfo(inspectFileName);
			if (stockFileInfo.Exists 
				&& (!inspectFileInfo.Exists
				|| stockFileInfo.LastWriteTime > inspectFileInfo.LastWriteTime))
			{
				System.IO.File.Copy(updateUri, inspectFileName, true);
				return inspectFileName;
			}

			return null;
		}
	}
}
