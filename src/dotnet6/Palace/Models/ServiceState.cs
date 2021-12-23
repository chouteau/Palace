using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Models
{
    public enum ServiceState
    {
        NotResponding = -3,
        NotExists = -2,
        NotInstalled = -1,
        Offline = 0,
        Starting = 1,
        StartFail = 2,
        Started = 3,
        UpdateDetected = 4,
        UpdateInProgress = 5,
        Updated = 6,
        InstallationFailed = 7,
        NotExitedAfterStop = 8
    }
}
