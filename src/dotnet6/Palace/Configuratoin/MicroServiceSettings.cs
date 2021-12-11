using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Configuration
{
	public class MicroServiceSettings
	{
		public string ServiceName { get; set; }
        public string MainFileName { get; set; }
        public string AdminServiceUrl { get; set; }
        public bool AlwaysStarted { get; set; }
        public string PalaceApiKey { get; set; }
    }
}
