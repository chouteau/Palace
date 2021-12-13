using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using PalaceServer.Models;

namespace Palace.Logging
{
    internal class PalaceLogger : ILogger
    {
		private readonly string _categoryName;

        public PalaceLogger(Configuration.PalaceSettings palaceSettings, string categoryName)
        {
            this.PalaceSettings = palaceSettings;
			this._categoryName = categoryName;
			this.Semaphore = new SemaphoreSlim(1, 1);
			this.WriteQueue = new System.Collections.Concurrent.ConcurrentQueue<LogInfo>();
		}

		protected Configuration.PalaceSettings PalaceSettings { get; }
		protected SemaphoreSlim Semaphore { get; }
		protected System.Collections.Concurrent.ConcurrentQueue<LogInfo> WriteQueue { get; }

		public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return PalaceSettings.LogLevel <= logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

			var logInfo = new LogInfo()
			{
				ApplicationName = "Palace",
				CreationDate = DateTime.Now,
				HostName = PalaceSettings.HostName,
				MachineName = System.Environment.MachineName,
				Context = _categoryName,
				Message = $"{formatter(state, exception)}",
			};

			logInfo.ExceptionStack = GetExceptionContent(exception);

			switch (logLevel)
			{
				case LogLevel.Trace:
					logInfo.Category = Category.Debug;
					break;
				case LogLevel.Debug:
					logInfo.Category = Category.Debug;
					break;
				case LogLevel.Information:
					logInfo.Category = Category.Info;
					break;
				case LogLevel.Warning:
					logInfo.Category = Category.Warn;
					break;
				case LogLevel.Error:
					logInfo.Category = Category.Error;
					break;
				case LogLevel.Critical:
					logInfo.Category = Category.Fatal;
					break;
				case LogLevel.None:
					logInfo.Category = Category.Debug;
					break;
				default:
					break;
			}

			WriteQueue.Enqueue(logInfo);
			if (Semaphore.CurrentCount < 2)
			{
				Dequeue();
			}

		}

		private void Dequeue()
		{
			while (true)
			{
				bool result = WriteQueue.TryDequeue(out LogInfo logInfo);
				if (result)
				{
					WriteInternal(logInfo);
					continue;
				}
				break;
			}
		}

		private void WriteInternal(LogInfo logInfo)
		{
			Semaphore.Wait();
			try
			{
				using var httpClient = new HttpClient();
				httpClient.Timeout = TimeSpan.FromSeconds(5);
				httpClient.BaseAddress = new Uri(PalaceSettings.UpdateServerUrl);
				httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {PalaceSettings.ApiKey}");
				httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"PalaceLogger ({System.Environment.OSVersion}; {System.Environment.MachineName}; {PalaceSettings.HostName})");
				var httpMessage = new HttpRequestMessage(HttpMethod.Post, "/api/logging/writelog");
				httpMessage.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(logInfo), Encoding.UTF8, "application/json");
				var response = httpClient.Send(httpMessage);
				response.EnsureSuccessStatusCode();
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				Semaphore.Release();
			}
			Dequeue();
		}

		private static string GetExceptionContent(Exception ex, int level = 0)
		{
			if (ex == null)
			{
				return null;
			}

			var content = new StringBuilder();
			content.Append("--------------------------------------------");
			content.AppendLine();
			content.AppendLine(ex.Message);
			content.AppendLine("--------------------------------------------");

			// Ajout des extensions d'erreur
			if (ex.Data != null
				&& ex.Data.Count > 0)
			{
				foreach (var item in ex.Data.Keys)
				{
					if (item != null && ex.Data != null && ex.Data[item] != null)
					{
						string data = string.Empty;
						try
						{
							data = ex.Data[item].ToString();
							content.AppendFormat("{0} = {1}", item, data);
						}
						catch { }
					}
					content.AppendLine();
				}
			}

			content.Append(ex.StackTrace);
			content.AppendLine();
			if (ex.InnerException != null)
			{
				content.Append("--------------------------------------------");
				content.AppendLine();
				content.Append("Inner Exception");
				content.AppendLine();
				content.Append(GetExceptionContent(ex.InnerException, level++));
			}
			return content.ToString();
		}
	}
}
