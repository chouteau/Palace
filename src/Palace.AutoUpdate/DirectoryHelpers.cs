using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate
{
	public static class DirectoryHelpers
	{
		public static string GetOrCreateStockDirectory()
		{
			var folder = GlobalConfiguration.CurrentFolder.Substring(2).Replace("\\", "-").Trim('-');
			var stockDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Palace", folder, "Stock");
			if (!System.IO.Directory.Exists(stockDirectory))
			{
				System.IO.Directory.CreateDirectory(stockDirectory);
			}
			return stockDirectory;
		}

		public static string GetOrCreateInspectDirectory()
		{
			var folder = GlobalConfiguration.CurrentFolder.Substring(2).Replace("\\", "-").Trim('-');
			var inspectDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Palace", folder, "Inspect");
			if (!System.IO.Directory.Exists(inspectDirectory))
			{
				System.IO.Directory.CreateDirectory(inspectDirectory);
			}
			return inspectDirectory;
		}

	}
}
