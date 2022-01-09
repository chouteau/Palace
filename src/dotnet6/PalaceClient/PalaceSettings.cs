using System;
using System.Collections.Generic;
using System.Text;

namespace PalaceClient
{
    public class PalaceSettings
    {
        public PalaceSettings()
        {
            StartedDate = DateTime.Now;
        }
        public string ApiKey { get; set; }
        public string ServiceName { get; set; }
        public string Version { get; set; }
        public string PalaceClientVersion { get; set; }
        public string Location { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime StartedDate { get; set; }
    }
}
