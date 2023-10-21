using ComposableAsync;
using MapMaven.DataGatherers.ScoreSaber.Data;
using MapMaven.DataGatherers.Shared;
using Microsoft.EntityFrameworkCore;
using RateLimiter;
using System.Diagnostics;
using System.Threading;

namespace MapMaven.DataGatherers.ScoreSaber
{
    public class Worker : BackgroundService
    {
        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly ScoreSaberScoresContext _db;

        private readonly TimeLimiter _scoreSaberRateLimit = TimeLimiter.GetFromMaxCountByInterval(380, TimeSpan.FromMinutes(1));
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

        private readonly ILogger<Worker> _logger;

        private readonly IHostApplicationLifetime _lifetime;

        public Worker(ILogger<Worker> logger, ScoreSaberScoresContext db, ScoreSaberApiClient scoreSaber, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _db = db;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;
            _scoreSaber = scoreSaber;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _db.Database.MigrateAsync();

            await GetAllPlayersAsync(stoppingToken);
            await GetPlayerScores();

            _lifetime.StopApplication();
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

            if (existingPlayerIds.Count >= firstPlayersPage.Metadata.Total)
            {
                _logger.LogInformation("All players already fetched.");
                return;
            }

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

        private async Task GetPlayerScores()
        {
            await _db.Leaderboards.LoadAsync();

            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            var requestTimes = new Queue<TimeSpan>();

            var totalPlayerCount = await _db.Players.CountAsync();

            var playersStillToGetScores = _db.Players
                .Where(p => !p.PlayerScores.Any())
                .Select(p => p.Id)
                .ToList()
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            var alreadyDone = totalPlayerCount - playersStillToGetScores.Count();

            using var progressReporter = new ProgressReporter(playersStillToGetScores.Count + alreadyDone, 1, _logger, alreadyDone);

            ICollection<PlayerScore> playerScoresPage = new List<PlayerScore>();
            List<PlayerScore> playerScores = new List<PlayerScore>();

            for (int i = 1; i <= playersStillToGetScores.Count; i++)
            {
                var player = playersStillToGetScores[i];

                var page = 1;
                var totalPlayerPages = 0D;

                do
                {
                    PlayerScoreCollection playersResult;

                    try
                    {
                        await _scoreSaberRateLimit;

                        playersResult = await _scoreSaber.Scores2Async(
                            playerId: player,
                            limit: 100,
                            sort: Sort.Recent,
                            page: page,
                            withMetadata: true
                        );
                    }
                    catch (ApiException ex)
                    {
                        if (ex.StatusCode != 404)
                            throw;

                        playersResult = new PlayerScoreCollection { PlayerScores = new List<PlayerScore>() };
                    }

                    playerScoresPage = playersResult.PlayerScores;

                    foreach (var playerScore in playerScoresPage)
                    {
                        playerScore.PlayerId = player;
                    }

                    playerScores.AddRange(playerScoresPage);

                    totalPlayerPages = playersResult.Metadata.Total / playersResult.Metadata.ItemsPerPage;

                    if (playersResult.Metadata != null)
                        _logger.LogInformation($"Fetched page {page}/{totalPlayerPages} from player: {player}");

                    page++;
                }
                while (page <= totalPlayerPages);

                foreach (var playerScore in playerScores)
                {
                    var existingLeaderboard = await _db.Leaderboards.FindAsync(playerScore.Leaderboard.Id);

                    if (existingLeaderboard != null)
                        playerScore.Leaderboard = existingLeaderboard;
                }

                _db.PlayerScores.AddRange(playerScores);
                await _db.SaveChangesAsync();

                playerScores.Clear();

                progressReporter.ReportProgress();
            }
        }

        //private async Task GetPlayerScores()
        //{
        //    await _db.Leaderboards.LoadAsync();

        //    var totalPlayerCount = await _db.Players.CountAsync();

        //    var playersStillToGetScores = _db.Players
        //        .Where(p => !p.PlayerScores.Any())
        //        .Select(p => p.Id)
        //        .ToList()
        //        .OrderBy(x => Guid.NewGuid()) // Randomize order to get a wide variety of scores
        //        .ToList();

        //    var alreadyDone = totalPlayerCount - playersStillToGetScores.Count();

        //    using var progressReporter = new ProgressReporter(playersStillToGetScores.Count + alreadyDone, 100, _logger, alreadyDone);

        //    var scoreRequests = playersStillToGetScores.Select(async player =>
        //    {
        //        await _dbSemaphore.WaitAsync();

        //        var page = 0;
        //        var totalPlayerPages = 0;

        //        var playerScores = new List<PlayerScore>();
        //        ICollection<PlayerScore> playerScoresPage = new List<PlayerScore>();

        //        do
        //        {
        //            await _scoreSaberRateLimit;

        //            PlayerScoreCollection playersResult;

        //            try
        //            {
        //                playersResult = await _scoreSaber.Scores2Async(
        //                    playerId: player,
        //                    limit: 100,
        //                    sort: Sort.Recent,
        //                    page: page,
        //                    withMetadata: true
        //                );
        //            }
        //            catch (ApiException ex)
        //            {
        //                if (ex.StatusCode != 404)
        //                    throw;

        //                playersResult = new PlayerScoreCollection { PlayerScores = new List<PlayerScore>() };
        //            }

        //            playerScoresPage = playersResult.PlayerScores;

        //            foreach (var playerScore in playerScoresPage)
        //            {
        //                playerScore.PlayerId = player;
        //            }

        //            playerScores.AddRange(playerScoresPage);

        //            totalPlayerPages = (int)Math.Ceiling(playersResult.Metadata.Total / playersResult.Metadata.ItemsPerPage);

        //            if (playersResult.Metadata != null)
        //                _logger.LogInformation($"Fetched page {page}/{totalPlayerPages} from player: {player}");

        //            page++;
        //        }
        //        while (page <= totalPlayerPages);

        //        try
        //        {
        //            foreach (var playerScore in playerScores)
        //            {
        //                var existingLeaderboard = await _db.Leaderboards.FindAsync(playerScore.Leaderboard.Id);

        //                if (existingLeaderboard != null)
        //                    playerScore.Leaderboard = existingLeaderboard;
        //            }

        //            _db.PlayerScores.AddRange(playerScores);
        //            await _db.SaveChangesAsync();
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //        finally
        //        {
        //            _dbSemaphore.Release();
        //        }

        //        playerScores.Clear();

        //        progressReporter.ReportProgress();
        //    });

        //    await Task.WhenAll(scoreRequests);
        //}
    }
}
