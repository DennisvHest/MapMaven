using ComposableAsync;
using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities;
using Microsoft.Extensions.Logging;
using RateLimiter;
using System.IO.Abstractions;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards
{
    public class BeatLeaderService : ILeaderboardProviderService
    {
        private readonly BeatLeaderApiClient _beatLeader;
        private readonly IApplicationSettingService _applicationSettingService;
        private readonly IApplicationEventService _applicationEventService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileSystem _fileSystem;

        private readonly ILogger<BeatLeaderService> _logger;

        private readonly BehaviorSubject<string?> _playerId = new(null);

        private readonly CachedValue<Dictionary<string, RankedMapInfoItem>> _rankedMaps;

        public LeaderboardProvider LeaderboardProviderName => LeaderboardProvider.BeatLeader;

        public string? PlayerId => _playerId.Value;

        public IObservable<string?> PlayerIdObservable => _playerId;

        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps => _rankedMaps.ValueObservable;

        public IObservable<PlayerProfile?> PlayerProfile { get; private set; }

        public IObservable<IEnumerable<PlayerScore>> PlayerScores { get; private set; }

        public IObservable<bool> Active { get; private set; }

        public const string PlayerIdSettingKey = "BeatLeaderPlayerId";

        private static readonly TimeLimiter _beatLeaderApiLimit = TimeLimiter.GetFromMaxCountByInterval(10, TimeSpan.FromSeconds(10));

        public BeatLeaderService(
            BeatLeaderApiClient beatLeader,
            IApplicationSettingService applicationSettingService,
            IApplicationEventService applicationEventService,
            IHttpClientFactory httpClientFactory,
            IFileSystem fileSystem,
            ILogger<BeatLeaderService> logger)
        {
            _beatLeader = beatLeader;
            _applicationSettingService = applicationSettingService;
            _applicationEventService = applicationEventService;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
            _logger = logger;

            var playerScores = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    return Observable.Return(Enumerable.Empty<PlayerScore>());

                return Observable.FromAsync(async () =>
                {
                    var playerScores = Enumerable.Empty<PlayerScore>();
                    int totalScores;
                    int page = 1;

                    do
                    {
                        await _beatLeaderApiLimit;

                        var scoreCollection = await _beatLeader.ScoresAsync(
                            id: playerId,
                            sortBy: "date",
                            order: default,
                            page: page,
                            count: 100,
                            search: default,
                            diff: default,
                            mode: default,
                            requirements: default,
                            scoreStatus: default,
                            leaderboardContext: (ApiClients.BeatLeader.LeaderboardContexts)Models.Data.Leaderboards.BeatLeader.LeaderboardContexts.General,
                            type: default,
                            modifiers: default,
                            stars_from: default,
                            stars_to: default,
                            time_from: default,
                            time_to: default,
                            eventId: default
                        );

                        totalScores = scoreCollection.Metadata.Total;
                        playerScores = playerScores.Concat(scoreCollection.Data.Select(s => new PlayerScore(s)));

                        page++;
                    }
                    while ((page - 1) * 100 < totalScores);

                    return playerScores;
                })
                .Catch((Exception exception) =>
                {
                    _applicationEventService.RaiseError(new ErrorEvent
                    {
                        Exception = exception,
                        Message = "Failed to load player scores from BeatLeader."
                    });

                    return Observable.Return(Enumerable.Empty<PlayerScore>());
                });
            }).Concat().Replay(1);

            playerScores.Connect();

            PlayerScores = playerScores;

            var playerProfile = _playerId
                .Select(playerId =>
                {
                    if (string.IsNullOrEmpty(playerId))
                        return Observable.Return(null as PlayerProfile);

                    return Observable.FromAsync(async () =>
                    {
                        await _beatLeaderApiLimit;

                        var playerProfile = await _beatLeader.PlayerAsync(
                            id: playerId,
                            stats: false,
                            keepOriginalId: false,
                            leaderboardContext: (ApiClients.BeatLeader.LeaderboardContexts)Models.Data.Leaderboards.BeatLeader.LeaderboardContexts.General
                        );

                        return new PlayerProfile(playerProfile);
                    })
                    .Catch((Exception exception) =>
                    {
                        _applicationEventService.RaiseError(new ErrorEvent
                        {
                            Exception = exception,
                            Message = "Failed to load player profile from BeatLeader."
                        });

                        return Observable.Return(null as PlayerProfile);
                    });
                })
                .Concat();

            var playerHistory = _playerId
                .Select(playerId =>
                {
                    if (string.IsNullOrEmpty(playerId))
                        return Observable.Return(Enumerable.Empty<RankHistoryRecord>());

                    return Observable.FromAsync(async () =>
                    {
                        await _beatLeaderApiLimit;

                        var history = await _beatLeader.HistoryAsync(
                            id: playerId,
                            leaderboardContext: (ApiClients.BeatLeader.LeaderboardContexts)Models.Data.Leaderboards.BeatLeader.LeaderboardContexts.General,
                            count: 50
                        );

                        return history
                            ?.Select(h => new RankHistoryRecord(h))
                            .OrderBy(h => h.Date) ?? Enumerable.Empty<RankHistoryRecord>();
                    })
                    .Catch((Exception exception) =>
                    {
                        _applicationEventService.RaiseError(new ErrorEvent
                        {
                            Exception = exception,
                            Message = "Failed to load player history from BeatLeader."
                        });

                        return Observable.Return(Enumerable.Empty<RankHistoryRecord>());
                    });
                }).Concat().Replay(1);

            playerHistory.Connect();

            PlayerProfile = Observable.CombineLatest(playerProfile, playerHistory, (profile, history) =>
            {
                if (profile is null)
                    return null;

                profile.RankHistory = history ?? Enumerable.Empty<RankHistoryRecord>();

                return profile;
            });

            _rankedMaps = new(GetRankedMaps, TimeSpan.FromHours(6), []);

            var playerId = _applicationSettingService.ApplicationSettings
                .Select(applicationSettings => applicationSettings.TryGetValue(PlayerIdSettingKey, out var playerId) ? playerId.StringValue : null)
                .DistinctUntilChanged();

            playerId.Subscribe(_playerId.OnNext);

            Active = playerId.Select(playerId => !string.IsNullOrEmpty(playerId));
        }

        public string? GetPlayerIdFromReplays(string beatSaberInstallLocation)
        {
            var beatLeaderReplaysLocation = Path.Combine(BeatSaberFileService.GetUserDataLocation(beatSaberInstallLocation), "BeatLeader", "Replays");

            if (!_fileSystem.Directory.Exists(beatLeaderReplaysLocation))
                return null;

            var replayFileName = _fileSystem.Directory.EnumerateFiles(beatLeaderReplaysLocation, "*.bsor").FirstOrDefault();

            if (string.IsNullOrEmpty(replayFileName))
                return null;

            var replayFile = _fileSystem.FileInfo.New(replayFileName);

            var playerId = replayFile.Name
                .Split('-')
                .First();

            return playerId;
        }

        public string? GetReplayUrl(string mapId, PlayerScore score)
        {
            if (!score.Score.HasReplay)
                return null;

            return $"{LeaderboardService.ReplayBaseUrl}/?scoreId={score.Score.Id}";
        }

        public void ReloadRankedMaps()
        {
            _rankedMaps.UpdateValue();
        }

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps()
        {
            try
            {
                _logger.LogInformation("Loading ranked maps for BeatLeader.");

                var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

                var response = await httpClient.GetFromJsonAsync<RankedMapInfo>("/beatleader/ranked-maps.json");

                return response?.RankedMaps ?? [];
            }
            catch (Exception ex)
            {
                _applicationEventService.RaiseError(new()
                {
                    Exception = ex,
                    Message = "Failed to load ranked maps from BeatLeader."
                });

                return _rankedMaps.Value;
            }
        }

        public void RefreshPlayerData()
        {
            _playerId.OnNext(_playerId.Value);
        }

        public async Task SetPlayerId(string playerId)
        {
            await _applicationSettingService.AddOrUpdateAsync(PlayerIdSettingKey, playerId);
        }
    }
}
