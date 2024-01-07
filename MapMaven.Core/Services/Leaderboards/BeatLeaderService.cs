using ComposableAsync;
using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
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

        private readonly BehaviorSubject<string?> _playerId = new(null);
        private readonly BehaviorSubject<Dictionary<string, RankedMapInfoItem>> _rankedMaps = new(new());

        public LeaderboardProvider LeaderboardProviderName => LeaderboardProvider.BeatLeader;

        public string? PlayerId => _playerId.Value;

        public IObservable<string?> PlayerIdObservable => _playerId;

        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps => _rankedMaps;

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
            IFileSystem fileSystem)
        {
            _beatLeader = beatLeader;
            _applicationSettingService = applicationSettingService;
            _applicationEventService = applicationEventService;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;

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
                        Message = "Failed to load player scores from ScoreSaber."
                    });

                    return Observable.Return(Enumerable.Empty<PlayerScore>());
                });
            }).Concat().Replay(1);

            playerScores.Connect();

            PlayerScores = playerScores;

            PlayerProfile = _playerId
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
                    });
                })
                .Concat();

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

        public async Task LoadRankedMaps()
        {
            try
            {
                var rankedMaps = await GetRankedMaps();

                _rankedMaps.OnNext(rankedMaps);
            }
            catch (Exception ex)
            {
                _applicationEventService.RaiseError(new()
                {
                    Exception = ex,
                    Message = "Failed to load ranked maps from BeatLeader."
                });

                _rankedMaps.OnNext(new());
            }
        }

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps()
        {
            var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

            var response = await httpClient.GetFromJsonAsync<RankedMapInfo>("/beatleader/ranked-maps.json");

            return response?.RankedMaps ?? new();
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
