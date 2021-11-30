using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PalaceClient
{
    public static class StopAwaiter
    {
        public static void Wait()
        {
            var mre = new ManualResetEvent(false);
            AppDomain.CurrentDomain.SetData("StopperEvent", mre);
            mre.WaitOne();
        }
    }
}
