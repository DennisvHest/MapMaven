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

        private readonly TimeLimiter _beatLeaderRateLimit = TimeLimiter.GetFromMaxCountByInterval(10, TimeSpan.FromSeconds(10));
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

            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            //await GetAllPlayersAsync(stoppingToken);
            await GetPlayerScores(stoppingToken);
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

                try
                {
                    await _dbSemaphore.WaitAsync();

                    foreach (var player in players.Data)
                    {
                        var existingPlayer = await _db.Players.FindAsync(player.Id);

                        if (existingPlayer is not null)
                            continue;

                        _db.Players.Add(player);
                    }

                    await _db.SaveChangesAsync(stoppingToken);
                }
                finally
                {
                    _dbSemaphore.Release();
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

        private async Task GetPlayerScores(CancellationToken stoppingToken)
        {
            var totalPlayerCount = await _db.Players.CountAsync(p => p.ScoreStats.RankedPlayCount > 0);

            var playersStillToGetScores = _db.Players
                .Where(p => !p.Scores.Any() && p.ScoreStats.RankedPlayCount > 0)
                .Select(p => p.Id)
                .ToList()
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            var alreadyDone = totalPlayerCount - playersStillToGetScores.Count();

            using var progressReporter = new ProgressReporter(playersStillToGetScores.Count + alreadyDone, 1, _logger, alreadyDone);

            ICollection<ScoreResponseWithMyScore> playerScoresPage = new List<ScoreResponseWithMyScore>();
            List<ScoreResponseWithMyScore> playerScores = new List<ScoreResponseWithMyScore>();

            for (int i = 1; i <= playersStillToGetScores.Count; i++)
            {
                var player = playersStillToGetScores[i];

                var page = 1;
                var totalPlayerPages = 0D;

                do
                {
                    ScoreResponseWithMyScoreResponseWithMetadata playerScoresResult;

                    try
                    {
                        await _beatLeaderRateLimit;

                        playerScoresResult = await _beatLeader.ScoresAsync(
                            id: player,
                            sortBy: "date",
                            order: Order._1, // Ascending
                            page: page,
                            count: 100,
                            search: default,
                            diff: default,
                            mode: default,
                            requirements: default,
                            scoreStatus: default,
                            leaderboardContext: LeaderboardContexts._2, // General
                            type: "ranked",
                            modifiers: default,
                            stars_from: default,
                            stars_to: default,
                            time_from: default,
                            time_to: default,
                            eventId: default,
                            cancellationToken: stoppingToken
                        );
                    }
                    catch (ApiException ex)
                    {
                        if (ex.StatusCode != 404)
                            throw;

                        playerScoresResult = new ScoreResponseWithMyScoreResponseWithMetadata { Data = new List<ScoreResponseWithMyScore>() };
                    }

                    playerScoresPage = playerScoresResult.Data;

                    foreach (var playerScore in playerScoresPage)
                    {
                        playerScore.PlayerId = player;
                    }

                    playerScores.AddRange(playerScoresPage);

                    totalPlayerPages = Math.Ceiling((double)playerScoresResult.Metadata.Total / playerScoresResult.Metadata.ItemsPerPage);

                    if (totalPlayerPages == 0)
                        totalPlayerPages = 1;

                    if (playerScoresResult.Metadata != null)
                        _logger.LogInformation($"Fetched page {page}/{totalPlayerPages} from player: {player}");

                    page++;
                }
                while (page <= totalPlayerPages);

                foreach (var playerScore in playerScores)
                {
                    var existingLeaderboard = await _db.Leaderboards.FindAsync(playerScore.Leaderboard.Id);

                    if (existingLeaderboard != null)
                        playerScore.Leaderboard = existingLeaderboard;

                    var existingSong = await _db.Songs.FindAsync(playerScore.Leaderboard.SongId ?? playerScore.Leaderboard.Song.Id);

                    if (existingSong != null)
                        playerScore.Leaderboard.Song = existingSong;

                    _db.Scores.Add(playerScore);
                }

                await _db.SaveChangesAsync(stoppingToken);

                playerScores.Clear();

                progressReporter.ReportProgress();
            }
        }
    }
}
