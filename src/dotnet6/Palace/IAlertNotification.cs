using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
    public interface IAlertNotification
    {
        void Notify(string message);
    }
}
