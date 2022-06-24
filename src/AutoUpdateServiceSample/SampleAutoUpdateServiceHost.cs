using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdateServiceSample
{
    public class SampleAutoServiceUpdatableHost : MarshalByRefObject, IDisposable
	{
		private System.Timers.Timer m_Timer;

		public override object InitializeLifetimeService()
		{
			return null;
		}

		public SampleAutoServiceUpdatableHost()
		{
			System.Diagnostics.Trace.AutoFlush = true;
			var console = new System.Diagnostics.ConsoleTraceListener();
			if (!System.Diagnostics.Trace.Listeners.Contains(console))
			{
				System.Diagnostics.Trace.Listeners.Add(console);
			}

			System.Diagnostics.Trace.WriteLine("Constructed");
		}

		public void Initialize()
		{
			System.Diagnostics.Trace.WriteLine("Autoupdate18 Initialized");
		}

		public void Start()
		{
			m_Timer = new System.Timers.Timer();
			m_Timer.Interval = 2 * 1000;
			m_Timer.Elapsed += (s, arg) =>
			{
				System.Diagnostics.Trace.WriteLine(string.Format("Autoupdate18 Sample : {0}", DateTime.Now));
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
