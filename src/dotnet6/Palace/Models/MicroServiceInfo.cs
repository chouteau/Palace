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
        public string Location { get; set; }
        public string MainFileName { get; set; }
        public bool LocalInstallationExists { get; set; } = false;
        public DateTime? LastWriteTime { get; set; }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Diagnostics.Process Process { get; set; }
        
        public ServiceState ServiceState { get; set; }
    }
}
