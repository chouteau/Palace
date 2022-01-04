using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PalaceClient
{
    public static class StopAwaiter
    {
        public const string PALACE_STOPPER_EVENT = "PalaceStopperEvent";

        public static void WaitForStopFromWebApi()
        {
            var mre = new ManualResetEvent(false);
            AppDomain.CurrentDomain.SetData(PALACE_STOPPER_EVENT, mre);
            mre.WaitOne();
        }

        public static Task WaitForStopFromWebApi(this Task task)
        {
            var mre = new ManualResetEvent(false);
            AppDomain.CurrentDomain.SetData(PALACE_STOPPER_EVENT, mre);
            task.ConfigureAwait(false).GetAwaiter();
            mre.WaitOne();
            return task;
        }

    }
}
