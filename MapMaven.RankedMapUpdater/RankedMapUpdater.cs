using MapMaven.RankedMapUpdater.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MapMaven.RankedMapUpdater
{
    public class RankedMapUpdater
    {
        private readonly ILogger _logger;

        private readonly RankedMapService _rankedMapService;

        public RankedMapUpdater(ILoggerFactory loggerFactory, RankedMapService rankedMapService)
        {
            _logger = loggerFactory.CreateLogger<RankedMapUpdater>();
            _rankedMapService = rankedMapService;
        }

        [Function("UpdateRankedMapsData")]
        public async Task Run(
#if DEBUG
            [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
#else
            [TimerTrigger($"0 0 3 * * *")]
#endif
            TimerInfo timerInfo, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating ranked maps data at: {DateTime.Now}");

            var lastRunDate = timerInfo.ScheduleStatus?.Last ?? DateTime.Now.AddDays(-1);

            await _rankedMapService.UpdateRankedMaps(lastRunDate, cancellationToken);

            _logger.LogInformation($"Next ranked maps update at: {timerInfo.ScheduleStatus?.Next}");
        }
    }
}
