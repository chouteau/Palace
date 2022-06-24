using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Configuration
{
	public class SmtpSettings
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public bool EnableSsl { get; set; }
		public string FromPersonEmail { get; set; }
		public string FromPersonName { get; set; }
		public string PickupDirectoryLocation { get; set; }
		public string DKIMPrivateKeyFileName { get; set; }
		public string DKIMPrivateKey { get; set; }
		public string DKIMPublicKey { get; set; }
		public string DKIMDomain { get; set; }
		public string DKIMSelector { get; set; }
		public string DKIMHeaders { get; set; }

		public static implicit operator System.Net.Mail.SmtpClient(SmtpSettings settings)
		{
			var result = new System.Net.Mail.SmtpClient();
			if (settings.Host == "file")
			{
				result.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
				if (settings.PickupDirectoryLocation.StartsWith(@".\", StringComparison.InvariantCultureIgnoreCase))
				{
					var currentPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TimeSheetMails");
					result.PickupDirectoryLocation = System.IO.Path.Combine(currentPath, settings.PickupDirectoryLocation.Replace(@".\", ""));
					if (!System.IO.Directory.Exists(result.PickupDirectoryLocation))
					{
						System.IO.Directory.CreateDirectory(result.PickupDirectoryLocation);
					}
				}
				else
				{
					result.PickupDirectoryLocation = settings.PickupDirectoryLocation;
				}
			}
			else
			{
				result.Host = settings.Host;
				result.Port = settings.Port;
				result.Credentials = new System.Net.NetworkCredential()
				{
					Password = settings.Password,
					UserName = settings.UserName,
				};
				result.EnableSsl = settings.EnableSsl;
			}
			result.Timeout = 30 * 1000;

			return result;

		}
	}
}
