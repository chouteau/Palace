namespace PalaceServer.Configuration
{
    public class PalaceServerSettings
    {
        public string AdminKey { get; set; }
        public string ApiKey { get; set; }
        public string MicroServiceRepositoryFolder { get; set; } = @".\Repository";
        public string MicroServiceConfigurationFolder { get; set; } = @".\Configuration";
        public string MicroServiceStagingFolder { get; set; } = @".\Staging";
        public string MicroServiceBackupFolder { get; set; } = @".\Backup";
    }
}
