﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                    BeatSaberDataService.CleanLargeObjectHeap();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred memory clean.");
                }
            }
        }
    }
}
