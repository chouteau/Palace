using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate
{
	public class MethodInvoker : MarshalByRefObject
	{
		public override object InitializeLifetimeService()
		{
			return null;
		}

		public void Invoke(object o, string methodName)
		{
			var method = o.GetType().GetMethod(methodName);
			method.Invoke(o, null);
		}
	}
}
