namespace DemoSvc 
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly List<string> _buffer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _buffer = new();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var version = $"{typeof(Program).Assembly.GetName()?.Version}";

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time} {version} {_buffer.Count}", DateTimeOffset.Now, version, _buffer.Count);
                // _buffer.Add("0".PadLeft(100000));
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}