using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities.Scoresaber;
using MapMaven_Core;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards
{
    public class ScoreSaberService : ILeaderboardProvider
    {
        public string LeaderboardProviderName => LeaderboardProviders.ScoreSaber;

        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly IApplicationSettingService _applicationSettingService;
        private readonly IApplicationEventService _applicationEventService;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BehaviorSubject<string?> _playerId = new(null);
        private readonly BehaviorSubject<Dictionary<string, RankedMapInfoItem>> _rankedMaps = new(new());

        public IObservable<string?> PlayerIdObservable => _playerId;
        public IObservable<PlayerProfile?> PlayerProfile { get; private set; }
        public IObservable<IEnumerable<PlayerScore>> PlayerScores { get; private set; }
        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; private set; }

        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps => _rankedMaps;

        public string? PlayerId => _playerId.Value;

        private const string PlayerIdSettingKey = "PlayerId";
        private const string _replayBaseUrl = "https://www.replay.beatleader.xyz";

        public ScoreSaberService(
            ScoreSaberApiClient scoreSaber,
            IHttpClientFactory httpClientFactory,
            IApplicationSettingService applicationSettingService,
            IApplicationEventService applicationEventService)
        {
            _scoreSaber = scoreSaber;
            _httpClientFactory = httpClientFactory;
            _applicationSettingService = applicationSettingService;
            _applicationEventService = applicationEventService;

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
                        var scoreCollection = await _scoreSaber.ScoresAsync(
                            playerId: playerId,
                            limit: 100,
                            sort: Sort.Top,
                            page: page,
                            withMetadata: true
                        );

                        totalScores = scoreCollection.Metadata.Total;

                        playerScores = playerScores.Concat(scoreCollection.PlayerScores);

                        page++;
                    }
                    while ((page - 1) * 100 < totalScores);

                    return playerScores;
                });
            }).Concat().Replay(1);

            playerScores.Connect();

            PlayerScores = playerScores.Catch((Exception exception) =>
            {
                _applicationEventService.RaiseError(new ErrorEvent
                {
                    Exception = exception,
                    Message = "Failed to load player scores from ScoreSaber."
                });

                return Observable.Return(Enumerable.Empty<PlayerScore>());
            });

            PlayerProfile = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    return Observable.Return(null as PlayerProfile);

                return Observable.FromAsync(async () =>
                {
                    var playerProfile = await _scoreSaber.FullAsync(playerId);

                    return new PlayerProfile(playerProfile);
                });
            }).Concat();

            PlayerProfile = PlayerProfile.Catch((Exception exception) =>
            {
                _applicationEventService.RaiseError(new ErrorEvent
                {
                    Exception = exception,
                    Message = "Failed to load player profile from ScoreSaber."
                });

                return Observable.Return(null as PlayerProfile);
            });

            var rankedMapScoreEstimates = PlayerProfile.CombineLatest(PlayerScores, RankedMaps, (player, playerScores, rankedMaps) =>
            {
                if (player == null)
                    return Enumerable.Empty<ScoreEstimate>();

                var scoresaber = new Scoresaber(player, playerScores);

                return rankedMaps.SelectMany(map =>
                {
                    return map.Value.Difficulties.Select(difficulty =>
                    {
                        var output = ScoreSaberScoreEstimateMLModel.Predict(new ScoreSaberScoreEstimateMLModel.ModelInput
                        {
                            PP = Convert.ToSingle(player.Pp),
                            StarDifficulty = Convert.ToSingle(difficulty.Stars),
                            TimeSet = DateTime.Now
                        });

                        return scoresaber.GetScoreEstimate(map.Value, difficulty, output.Score);
                    });
                }).ToList();
            }).Replay(1);

            rankedMapScoreEstimates.Connect();

            RankedMapScoreEstimates = rankedMapScoreEstimates;

            _applicationSettingService.ApplicationSettings
                .Select(applicationSettings => applicationSettings.TryGetValue(PlayerIdSettingKey, out var playerId) ? playerId.StringValue : null)
                .DistinctUntilChanged()
                .Subscribe(_playerId.OnNext);
        }

        public async Task SetPlayerId(string playerId)
        {
            await _applicationSettingService.AddOrUpdateAsync(PlayerIdSettingKey, playerId);
        }

        public void RefreshPlayerData()
        {
            _playerId.OnNext(_playerId.Value);
        }

        public string? GetPlayerIdFromReplays(string beatSaberInstallLocation)
        {
            var scoreSaberReplaysLocation = Path.Combine(BeatSaberFileService.GetUserDataLocation(beatSaberInstallLocation), "ScoreSaber", "Replays");

            if (!Directory.Exists(scoreSaberReplaysLocation))
                return null;

            var replayFileName = Directory.EnumerateFiles(scoreSaberReplaysLocation, "*.dat").FirstOrDefault();

            if (string.IsNullOrEmpty(replayFileName))
                return null;

            var replayFile = new FileInfo(replayFileName);

            var playerId = replayFile.Name
                .Split('-')
                .First();

            return playerId;
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
                    Message = "Failed to load ranked maps from ScoreSaber."
                });

                _rankedMaps.OnNext(new());
            }
        }

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps()
        {
            var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

            var response = await httpClient.GetFromJsonAsync<RankedMapInfo>("/scoresaber/ranked-maps.json");

            return response?.RankedMaps ?? new();
        }

        public string? GetReplayUrl(string mapId, PlayerScore score)
        {
            if (!score.Score.HasReplay)
                return null;

            return $"{_replayBaseUrl}/?id={mapId}&difficulty={score.Leaderboard.Difficulty.DifficultyName}&playerID={_playerId.Value}";
        }
    }
}
