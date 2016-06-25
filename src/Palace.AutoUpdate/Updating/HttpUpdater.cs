using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Palace.AutoUpdate.Updating
{
	public class HttpUpdater : UpdaterBase
	{
		public override string CheckAndGet(string updateUri)
		{
			var fileName = System.IO.Path.GetFileName(updateUri);
			fileName = System.IO.Path.Combine(GlobalConfiguration.GetOrCreateStockDirectory(), fileName);
			var currentFile = new System.IO.FileInfo(updateUri);
			var lastWriteDate = DateTime.MinValue;
			if (currentFile.Exists)
			{
				lastWriteDate = currentFile.LastWriteTime;
			}

			var httpClient = new HttpClient();
			var ci = new System.Globalization.CultureInfo("en-US");
			httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoUpdateSvcHost/1.0 (+http://www.serialcoder.net)");
			httpClient.DefaultRequestHeaders.Add("If-Modified-Since", string.Format(ci, "{0:ddd, dd MMM yyyy HH:mm:ss} GMT", lastWriteDate.ToUniversalTime()));
			httpClient.DefaultRequestHeaders.Add("ApiKey", GlobalConfiguration.Settings.ApiKey);

			var response = httpClient.GetAsync(updateUri).Result;

			if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
			{
				return null;
			}

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new UpdateUrlNotAccessibleException();
			}

			System.IO.File.Delete(fileName);
			using (var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
			{
				var stream = response.Content.ReadAsStreamAsync().Result;
				int bufferSize = 1024;
				byte[] buffer = new byte[bufferSize];
				int pos = 0;
				while ((pos = stream.Read(buffer, 0, bufferSize)) > 0)
				{
					fs.Write(buffer, 0, pos);
				}
				fs.Close();
			}
			return fileName;
		}

	}
}
