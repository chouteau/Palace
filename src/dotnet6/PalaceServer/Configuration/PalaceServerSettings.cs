namespace PalaceServer.Configuration
{
    public class PalaceServerSettings
    {
        public string ApiKey { get; set; }
        public string MicroServiceRepositoryFolder { get; set; }
        public int LogCountMax { get; set; } = 20000;
    }
}
