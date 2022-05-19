using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Services
{
	internal class SmtpNotificationService : INotificationService
	{
		public SmtpNotificationService(Configuration.SmtpSettings smtpSettings,
			Configuration.PalaceSettings palaceSettings)
		{
			this.SmtpSettings = smtpSettings;
			this.PalaceSettings = palaceSettings;
		}

		protected Configuration.SmtpSettings SmtpSettings { get; }
		protected Configuration.PalaceSettings PalaceSettings { get; }

		public async Task SendAlert(string message)
		{
			await SendInternal("[Palace] Alert !", message);
		}

		public async Task SendNotification(string message)
		{
			await SendInternal("[Palace] notification", message);
		}

		private async Task SendInternal(string subject, string message)
		{
			if (SmtpSettings == null
				|| string.IsNullOrWhiteSpace(SmtpSettings.Host))
			{
				return;
			}

			var mail = new System.Net.Mail.MailMessage();
			mail.From = new System.Net.Mail.MailAddress(SmtpSettings.FromPersonEmail, SmtpSettings.FromPersonName);
			mail.Subject = subject;
			mail.IsBodyHtml = false;
			var body = new System.Text.StringBuilder(message);
			body.AppendLine($"MachineName : {System.Environment.MachineName}");
			body.AppendLine($"HostName : {PalaceSettings.HostName}");
			body.AppendLine($"Stack : {System.Environment.StackTrace}");
			mail.Body = body.ToString();

			await ((System.Net.Mail.SmtpClient)SmtpSettings).SendMailAsync(mail);
		}
	}
}
