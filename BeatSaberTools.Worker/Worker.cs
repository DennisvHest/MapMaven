using BeatSaberTools.Core.Services;
using System.Diagnostics;

namespace BeatSaberTools.Worker
{
    public class Worker : BackgroundService
    {
        private readonly PeriodicTimer _timer = new PeriodicTimer(Debugger.IsAttached ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(5));

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<Worker> _logger;
        

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            do
            {
                try
                {
                    _logger.LogInformation("Arranging dynamic playlists...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dynamicPlaylistArrangementService = scope.ServiceProvider.GetRequiredService<DynamicPlaylistArrangementService>();

                        await dynamicPlaylistArrangementService.ArrangeDynamicPlaylists();
                    }

                    _logger.LogInformation("Done arranging dynamic playlists!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in worker");
                }
            }
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested);
        }


    }
}