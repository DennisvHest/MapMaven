using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapMaven.Services.Workers
{
    public class UpdateWorker : BackgroundService
    {
        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromHours(1));

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<UpdateWorker> _logger;


        public UpdateWorker(ILogger<UpdateWorker> logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
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
            }
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested);
        }
    }
}
