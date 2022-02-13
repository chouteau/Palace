using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Extensions
{
    public static class GlobalExtensions
    {
        public static void Initialize(this Configuration.PalaceSettings palaceSettings)
        {
            var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
            if (palaceSettings.BackupDirectory.StartsWith(@".\"))
            {
                palaceSettings.BackupDirectory = System.IO.Path.Combine(currentDirectory, palaceSettings.BackupDirectory.Replace(@".\", string.Empty));
            }
            if (palaceSettings.UpdateDirectory.StartsWith(@".\"))
            {
                palaceSettings.UpdateDirectory = System.IO.Path.Combine(currentDirectory, palaceSettings.UpdateDirectory.Replace(@".\", string.Empty));
            }
            if (palaceSettings.DownloadDirectory.StartsWith(@".\"))
            {
                palaceSettings.DownloadDirectory = System.IO.Path.Combine(currentDirectory, palaceSettings.DownloadDirectory.Replace(@".\", string.Empty));
            }
            if (palaceSettings.InstallationDirectory.StartsWith(@".\"))
            {
                palaceSettings.InstallationDirectory = System.IO.Path.Combine(currentDirectory, palaceSettings.InstallationDirectory.Replace(@".\", string.Empty));
            }
        }
    }
}
