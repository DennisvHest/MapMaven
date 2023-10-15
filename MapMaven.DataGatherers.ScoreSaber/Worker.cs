using MapMaven.DataGatherers.ScoreSaber.Data;
using Microsoft.EntityFrameworkCore;

namespace MapMaven.DataGatherers.ScoreSaber
{
    public class Worker : BackgroundService
    {
        private readonly ScoreSaberApiClient scoreSaberApiClient;
        private readonly ScoreSaberScoresContext db;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, ScoreSaberScoresContext db)
        {
            _logger = logger;
            this.db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await db.Database.MigrateAsync();
        }
    }
}
