using ComposableAsync;
using MapMaven.DataGatherers.ScoreSaber.Data;
using MapMaven.DataGatherers.Shared;
using Microsoft.EntityFrameworkCore;
using RateLimiter;

namespace MapMaven.DataGatherers.ScoreSaber
{
    public class Worker : BackgroundService
    {
        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly ScoreSaberScoresContext _db;

        private readonly TimeLimiter _scoreSaberRateLimit = TimeLimiter.GetFromMaxCountByInterval(380, TimeSpan.FromMinutes(1));
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, ScoreSaberScoresContext db, ScoreSaberApiClient scoreSaber)
        {
            _logger = logger;
            _db = db;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;
            _scoreSaber = scoreSaber;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _db.Database.MigrateAsync();

            await GetAllPlayersAsync(stoppingToken);
        }

        private async Task GetAllPlayersAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all players...");

            var existingPlayerIds = _db.Players.Select(x => x.Id).ToHashSet();

            var firstPlayersPage = await _scoreSaber.PlayersAsync(
                search: string.Empty,
                page: 1,
                countries: string.Empty,
                withMetadata: true,
                cancellationToken: cancellationToken
            );

            using var progressReporter = new ProgressReporter(firstPlayersPage.Metadata.Total, (int)firstPlayersPage.Metadata.ItemsPerPage, _logger);

            var totalPages = (int)Math.Ceiling(firstPlayersPage.Metadata.Total / firstPlayersPage.Metadata.ItemsPerPage);

            var playerRequests = Enumerable.Range(1, totalPages).Select(async page =>
            {
                await _scoreSaberRateLimit;

                var players = await _scoreSaber.PlayersAsync(
                    search: string.Empty,
                    page: page,
                    countries: string.Empty,
                    withMetadata: false
                );

                if (!players.Players.Any(x => existingPlayerIds.Contains(x.Id)))
                {
                    await _dbSemaphore.WaitAsync();

                    try
                    {
                        _db.Players.AddRange(players.Players);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                    finally
                    {
                        _dbSemaphore.Release();
                    }
                }

                progressReporter.ReportProgress();
            });

            await Task.WhenAll(playerRequests);

            _logger.LogInformation("Done fetching all players.");
        }
    }
}
