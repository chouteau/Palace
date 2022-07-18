namespace PalaceServer.Extensions;

public static class ConfigurationExtensions
{
	public static void PrepareFolders(this Configuration.PalaceServerSettings palaceSettings)
	{
		var directoryName = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
		if (palaceSettings.MicroServiceRepositoryFolder.StartsWith(@".\"))
		{
			palaceSettings.MicroServiceRepositoryFolder = System.IO.Path.Combine(directoryName, palaceSettings.MicroServiceRepositoryFolder.Replace(@".\", ""));
		}
		if (palaceSettings.MicroServiceStagingFolder.StartsWith(@".\"))
		{
			palaceSettings.MicroServiceStagingFolder = System.IO.Path.Combine(directoryName, palaceSettings.MicroServiceStagingFolder.Replace(@".\", ""));
		}
		if (palaceSettings.MicroServiceBackupFolder.StartsWith(@".\"))
		{
			palaceSettings.MicroServiceBackupFolder = System.IO.Path.Combine(directoryName, palaceSettings.MicroServiceBackupFolder.Replace(@".\", ""));
		}
		if (palaceSettings.MicroServiceConfigurationFolder.StartsWith(@".\"))
		{
			palaceSettings.MicroServiceConfigurationFolder = System.IO.Path.Combine(directoryName, palaceSettings.MicroServiceConfigurationFolder.Replace(@".\", ""));
		}

		if (!System.IO.Directory.Exists(palaceSettings.MicroServiceRepositoryFolder))
		{
			System.IO.Directory.CreateDirectory(palaceSettings.MicroServiceRepositoryFolder);
		}
		if (!System.IO.Directory.Exists(palaceSettings.MicroServiceStagingFolder))
		{
			System.IO.Directory.CreateDirectory(palaceSettings.MicroServiceStagingFolder);
		}
		if (!System.IO.Directory.Exists(palaceSettings.MicroServiceBackupFolder))
		{
			System.IO.Directory.CreateDirectory(palaceSettings.MicroServiceBackupFolder);
		}
		if (!System.IO.Directory.Exists(palaceSettings.MicroServiceConfigurationFolder))
		{
			System.IO.Directory.CreateDirectory(palaceSettings.MicroServiceConfigurationFolder);
		}

	}
}
