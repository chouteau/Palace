namespace DemoSvc
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var version = $"{typeof(Program).Assembly.GetName()?.Version}";

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time} {version}", DateTimeOffset.Now, version);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}