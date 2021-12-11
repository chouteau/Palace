namespace PalaceServer.Models
{
    public class ExtendedRunningMicroServiceInfo : PalaceClient.RunningMicroserviceInfo
    {
        public ExtendedRunningMicroServiceInfo()
        {
            CreationDate = DateTime.Now;
            LastUpdateDate = DateTime.Now;
        }

        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public PalaceInfo PalaceInfo { get; set; }
        public ServiceAction NextAction { get; set; }

        public string Key
        {
            get
            {
                return $"{PalaceInfo.MachineName}.{PalaceInfo.HostName}.{ServiceName}".ToLower();
            }
        }
    }
}
