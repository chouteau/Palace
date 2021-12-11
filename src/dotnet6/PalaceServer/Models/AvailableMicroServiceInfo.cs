namespace PalaceServer.Models
{
    public class AvailableMicroServiceInfo
    {
        public string ServiceName { get; set; }
        public string ZipFileName { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string LockedBy { get; set; }
    }
}
