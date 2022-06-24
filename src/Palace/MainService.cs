using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Palace
{
	public partial class MainService : ServiceBase
	{
		private Starter m_LoaderService;

		public MainService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			m_LoaderService = new Starter();
			m_LoaderService.Start();
		}

		protected override void OnStop()
		{
			if (m_LoaderService != null)
			{
				m_LoaderService.Stop();
			}
		}
	}
}
