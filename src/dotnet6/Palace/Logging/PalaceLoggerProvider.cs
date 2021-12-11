using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Logging
{
    internal class PalaceLoggerProvider : ILoggerProvider
    {
        public PalaceLoggerProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        protected IServiceProvider ServiceProvider { get; }

        public ILogger CreateLogger(string categoryName)
        {
            var settings = ServiceProvider.GetRequiredService<Configuration.PalaceSettings>();
            return new PalaceLogger(settings, categoryName);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
