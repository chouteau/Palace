using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	public static class GlobalConfiguration
	{
		private static string m_ConfigurationFileName;
		private static Lazy<Settings> m_LazySettings = new Lazy<Settings>(() =>
		{
			var settings = GetSettings();
			return settings;
		}, true);

		static GlobalConfiguration()
		{
			CurrentFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			m_ConfigurationFileName = System.IO.Path.Combine(CurrentFolder, "palace.config.json");
		}

		public static Settings Settings
		{
			get
			{
				return m_LazySettings.Value;
			}
		}

		public static string CurrentFolder { get; private set; }

		public static string GetOrCreateStockDirectory()
		{
			var stockDirectory = System.IO.Path.Combine(CurrentFolder, "Stock");
			if (!System.IO.Directory.Exists(stockDirectory))
			{
				System.IO.Directory.CreateDirectory(stockDirectory);
			}
			return stockDirectory;
		}

		public static string GetOrCreateInspectDirectory()
		{
			var stockDirectory = System.IO.Path.Combine(CurrentFolder, "Inspect");
			if (!System.IO.Directory.Exists(stockDirectory))
			{
				System.IO.Directory.CreateDirectory(stockDirectory);
			}
			return stockDirectory;
		}

		public static void SaveSettings()
		{
			var content = Newtonsoft.Json.JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
			System.IO.File.WriteAllText(m_ConfigurationFileName, content);
		}

		private static Settings GetSettings()
		{
			var result = new Settings();
			if (System.IO.File.Exists(m_ConfigurationFileName))
			{
				var content = System.IO.File.ReadAllText(m_ConfigurationFileName);
				result = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(content);
			}
			return result;
		}
	}
}
