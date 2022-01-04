using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalaceServer.Logging
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
            var logCollector = ServiceProvider.GetRequiredService<Services.LogCollector>();
            return new PalaceLogger(categoryName, logCollector);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
