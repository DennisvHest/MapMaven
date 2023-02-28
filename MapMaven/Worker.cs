using MapMaven.Core.Services;
using MapMaven.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapMaven.Worker
{
    public class Worker : BackgroundService
    {
        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<Worker> _logger;


        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
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
                    var updateService = _serviceProvider.GetRequiredService<UpdateService>();

                    await updateService.CheckForUpdates();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during update check.");
                }

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
        }
    }
}