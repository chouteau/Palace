using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceSample
{
    public class SampleServiceHost : IDisposable
    {
		private System.Timers.Timer m_Timer;

		public void Start()
		{
			m_Timer = new System.Timers.Timer();
			m_Timer.Interval = 2 * 1000;
			m_Timer.Elapsed += (s, arg) =>
			{
				System.Diagnostics.Trace.WriteLine(String.Format("SampleService : {0}", DateTime.Now));
			};
			m_Timer.Start();
		}

		public void Initialize()
		{
			System.Diagnostics.Trace.WriteLine("SampleService : Initialized");
		}

		public void Stop()
		{
			m_Timer.Stop();
		}

		public void Dispose()
		{
			m_Timer = null;
		}

	}
}
