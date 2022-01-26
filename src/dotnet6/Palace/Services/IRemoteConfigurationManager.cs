using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
    public interface IRemoteConfigurationManager
    {
        Task<IEnumerable<Models.MicroServiceSettings>> SynchronizeConfiguration(IEnumerable<Models.MicroServiceSettings> list);
    }
}
