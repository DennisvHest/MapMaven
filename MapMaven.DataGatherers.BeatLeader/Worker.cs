using ComposableAsync;
using MapMaven.DataGatherers.BeatLeader.Data;
using MapMaven.DataGatherers.Shared;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RateLimiter;

namespace MapMaven.DataGatherers.BeatLeader
{
    public class Worker : BackgroundService
    {
        private readonly BeatLeaderApiClient _beatLeader;
        private readonly BeatLeaderScoresContext _db;

        private readonly TimeLimiter _beatLeaderRateLimit = TimeLimiter.GetFromMaxCountByInterval(18, TimeSpan.FromSeconds(10));
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, BeatLeaderApiClient beatLeader, BeatLeaderScoresContext db)
        {
            _logger = logger;
            _beatLeader = beatLeader;
            _db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _db.Database.MigrateAsync();

            await GetAllPlayersAsync(stoppingToken);
        }

        private async Task GetAllPlayersAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Fetching all players...");

            var existingPlayerIds = _db.Players.Select(x => x.Id).ToHashSet();

            var firstPlayersPage = await PlayersRequest(1, stoppingToken);

            if (existingPlayerIds.Count >= firstPlayersPage.Metadata.Total)
            {
                _logger.LogInformation("All players already fetched.");
                return;
            }

            using var progressReporter = new ProgressReporter(firstPlayersPage.Metadata.Total, firstPlayersPage.Metadata.ItemsPerPage, _logger);

            var totalPages = (int)Math.Ceiling((double)firstPlayersPage.Metadata.Total / firstPlayersPage.Metadata.ItemsPerPage);

            var playerRequests = Enumerable.Range(1, totalPages).Select(async page =>
            {
                await _beatLeaderRateLimit;

                var players = await PlayersRequest(page, stoppingToken);

                if (!players.Data.Any(x => existingPlayerIds.Contains(x.Id)))
                {
                    await _dbSemaphore.WaitAsync();

                    try
                    {
                        _db.Players.AddRange(players.Data);
                        await _db.SaveChangesAsync(stoppingToken);
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

        private async Task<PlayerResponseWithStatsResponseWithMetadata> PlayersRequest(int page, CancellationToken stoppingToken)
        {
            return await _beatLeader.PlayersAsync(
                sortBy: "name",
                page: page,
                count: 100,
                search: default,
                order: Order._1, // Ascending
                countries: default,
                mapsType: "ranked",
                ppType: default,
                leaderboardContext: LeaderboardContexts._2, // General
                friends: false,
                pp_range: default,
                score_range: default,
                platform: default,
                role: default,
                hmd: default,
                clans: default,
                activityPeriod: default,
                banned: default,
                cancellationToken: stoppingToken
            );
        }
    }
}
