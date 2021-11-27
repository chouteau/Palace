using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Palace
{
	public class AssemblyInspector : MarshalByRefObject
	{
		public override object InitializeLifetimeService()
		{
			return null;
		}

		public IList<string> Inspect(string file, string suffix)
		{
			var assembly = System.Reflection.Assembly.LoadFrom(file);
			if (assembly.IsDynamic
				|| assembly.GlobalAssemblyCache
				|| assembly.Location.IndexOf("GAC_MSIL") != -1)
			{
				return null;
			}
			var typelist = from type in assembly.GetExportedTypes()
							let methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
							where type.Name.EndsWith(suffix)
								&& methods.Select(i => i.Name).Contains("Initialize")
								&& methods.Select(i => i.Name).Contains("Start")
								&& methods.Select(i => i.Name).Contains("Stop")
							    && type.FullName.IndexOf("microsoft.", StringComparison.InvariantCultureIgnoreCase) == -1
							    && type.FullName.IndexOf("system.", StringComparison.InvariantCultureIgnoreCase) == -1
							select type.AssemblyQualifiedName;

			return typelist.ToList();
		}

		~AssemblyInspector()
		{
			System.Runtime.Remoting.RemotingServices.Disconnect(this);
		}
	}
}
