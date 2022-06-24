using System.Linq.Expressions;

namespace PalaceServer.Models
{
    public class PalaceInfo
    {
        public PalaceInfo()
        {
			LastHitDate = DateTime.Now;
            MicroServiceSettingsList = new List<MicroServiceSettings>();
        }

        public string Os { get; set; }
        public string MachineName { get; set; }
        public string HostName { get; set; }
        public string Version { get; set; }
        public string Ip { get; set; }
        public DateTime LastHitDate { get; set; }

		public IEnumerable<MicroServiceSettings> MicroServiceSettingsList { get; set; }
        public DateTime? LastConfigurationUpdate { get; set; }

        public string Key => $"{MachineName}.{HostName}";
    }
}
