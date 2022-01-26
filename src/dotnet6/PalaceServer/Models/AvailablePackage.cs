namespace PalaceServer.Models
{
    public class AvailablePackage
    {
        public string PackageFileName { get; set; }
        public DateTime LastWriteTime { get; set; }
        public long Size { get; set; }
        public string LockedBy { get; set; }
    }
}
