using MapMaven.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapMaven.Services.Workers
{
    public class LivePlaylistWorker : BackgroundService
    {
        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<LivePlaylistWorker> _logger;


        public LivePlaylistWorker(ILogger<LivePlaylistWorker> logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Arranging live playlists...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var LivePlaylistArrangementService = scope.ServiceProvider.GetRequiredService<LivePlaylistArrangementService>();

                        await LivePlaylistArrangementService.ArrangeLivePlaylists();
                    }

                    _logger.LogInformation("Done arranging live playlists!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in worker");
                }
            }
        }
    }
}