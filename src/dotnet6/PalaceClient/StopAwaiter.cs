using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public static void Wait(Task task)
        {
            var mre = new ManualResetEvent(false);
            AppDomain.CurrentDomain.SetData("StopperEvent", mre);
            task.ConfigureAwait(false).GetAwaiter();
            mre.WaitOne();
        }

    }
}
