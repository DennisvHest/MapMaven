using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MapMaven.RankedMapUpdater
{
    public class RankedMapUpdater
    {
        private readonly ILogger _logger;

        public RankedMapUpdater(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RankedMapUpdater>();
        }

        [Function("UpdateRankedMapsData")]
        public void Run(
#if DEBUG
            [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
#else
            [TimerTrigger("0 0 3 * * *")]
#endif
            TimerInfo timerInfo)
        {
            _logger.LogInformation($"Updating ranked maps data at: {DateTime.Now}");



            _logger.LogInformation($"Next ranked maps update at: {timerInfo.ScheduleStatus.Next}");
        }
    }
}
