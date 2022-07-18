using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Models
{
    public class MicroServiceInfo
    {
        public string Version { get; set; }
        public string Name { get; set; }
        public string InstallationFolder { get; set; }
        public string MainFileName { get; set; }
        public string Arguments { get; set; }
        public bool LocalInstallationExists { get; set; } = false;
        public DateTime? LastWriteTime { get; set; }
        public bool InstallationFailed { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Diagnostics.Process Process { get; set; }
        
        public ServiceState ServiceState { get; set; }
        public string StartFailedMessage { get; set; }
		public int NotRespondingCount { get; set; }
	}
}
