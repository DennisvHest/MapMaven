using BeatSaberTools.Core.Services;
using System.Diagnostics;

namespace BeatSaberTools.Worker
{
    public class Worker : BackgroundService
    {
        private readonly DynamicPlaylistArrangementService _dynamicPlaylistArrangementService;

        private readonly PeriodicTimer _timer = new PeriodicTimer(Debugger.IsAttached ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(5));

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, DynamicPlaylistArrangementService dynamicPlaylistArrangementService)
        {
            _logger = logger;
            _dynamicPlaylistArrangementService = dynamicPlaylistArrangementService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            do
            {
                _logger.LogInformation("Gathering recently added maps...");

                await _dynamicPlaylistArrangementService.ArrangeDynamicPlaylists();

                _logger.LogInformation("Created playlist with recently added maps!");
            }
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested);
        }


    }
}