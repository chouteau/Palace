using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdateServiceSample
{
    public class SampleAutoUpdateServiceHost : MarshalByRefObject, IDisposable
	{
		private System.Timers.Timer m_Timer;

		public override object InitializeLifetimeService()
		{
			return null;
		}

		public SampleAutoUpdateServiceHost()
		{
			System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
			System.Diagnostics.Trace.AutoFlush = true;
		}

		public void Initialize()
		{
			System.Diagnostics.Trace.WriteLine("Autoupdate5 Initialized");
		}

		public void Start()
		{
			m_Timer = new System.Timers.Timer();
			m_Timer.Interval = 2 * 1000;
			m_Timer.Elapsed += (s, arg) =>
			{
				System.Diagnostics.Trace.WriteLine(string.Format("Autoupdate5 Sample : {0}", DateTime.Now));
			};
			m_Timer.Start();
		}

		public void Stop()
		{
			m_Timer.Stop();
		}

		public void Dispose()
		{
			m_Timer.Dispose();
			m_Timer = null;
		}
    }
}
