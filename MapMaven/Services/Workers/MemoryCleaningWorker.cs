using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime;

namespace MapMaven.Services.Workers
{
    public class MemoryCleaningWorker : BackgroundService
    {
        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(10));

        private readonly ILogger<MemoryCleaningWorker> _logger;


        public MemoryCleaningWorker(ILogger<MemoryCleaningWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred memory clean.");
                }
            }
        }
    }
}
