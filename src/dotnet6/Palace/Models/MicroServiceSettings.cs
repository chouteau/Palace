﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Models
{
	public class MicroServiceSettings
	{
		public string ServiceName { get; set; }
        public string MainAssembly { get; set; }
        public string InstallationFolder { get; set; }
        public string Arguments { get; set; }
        public string AdminServiceUrl { get; set; }
        public bool AlwaysStarted { get; set; }
        public string PalaceApiKey { get; set; }
        public string PackageFileName { get; set; }
        public string SSLCertificate { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool InstallationFailed { get; set; }
    }
}