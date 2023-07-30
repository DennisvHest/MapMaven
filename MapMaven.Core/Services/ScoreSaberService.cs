using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities.Scoresaber;
using MapMaven_Core;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services
{
    public class ScoreSaberService : IScoreSaberService
    {
        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly IApplicationSettingService _applicationSettingService;
        private readonly IApplicationEventService _applicationEventService;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BehaviorSubject<string?> _playerId = new(null);
        private readonly BehaviorSubject<IEnumerable<RankedMap>> _rankedMaps = new(Enumerable.Empty<RankedMap>());

        public IObservable<string?> PlayerIdObservable => _playerId;
        public IObservable<Player?> PlayerProfile { get; private set; }
        public IObservable<IEnumerable<PlayerScore>> PlayerScores { get; private set; }
        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; private set; }

        public IObservable<IEnumerable<RankedMap>> RankedMaps => _rankedMaps;

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
                _applicationEventService.RaiseError(new Models.ErrorEvent
                {
                    Exception = exception,
                    Message = "Failed to load player scores from ScoreSaber."
                });

                return Observable.Return(Enumerable.Empty<PlayerScore>());
            });

            PlayerProfile = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    return Observable.Return(null as Player);

                return Observable.FromAsync(async () => await _scoreSaber.FullAsync(playerId));
            }).Concat();


            PlayerProfile = PlayerProfile.Catch((Exception exception) =>
            {
                _applicationEventService.RaiseError(new Models.ErrorEvent
                {
                    Exception = exception,
                    Message = "Failed to load player profile from ScoreSaber."
                });

                return Observable.Return(null as Player);
            });

            var rankedMapScoreEstimates = Observable.CombineLatest(PlayerProfile, PlayerScores, RankedMaps, (player, playerScores, rankedMaps) =>
            {
                if (player == null)
                    return Enumerable.Empty<ScoreEstimate>();

                var scoresaber = new Scoresaber(player, playerScores);

                return rankedMaps.Select(map =>
                {
                    var output = ScoreEstimateMLModel.Predict(new ScoreEstimateMLModel.ModelInput
                    {
                        PP = Convert.ToSingle(player.Pp),
                        StarDifficulty = Convert.ToSingle(map.Stars),
                        TimeSet = DateTime.Now
                    });

                    return scoresaber.GetScoreEstimate(map, output.Score);
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
                    Message = "Failed to load ranked maps."
                });

                _rankedMaps.OnNext(Enumerable.Empty<RankedMap>());
            }
        }

        public async Task<IEnumerable<RankedMap>> GetRankedMaps()
        {
            var httpClient = _httpClientFactory.CreateClient("RankedScoresaber");

            var response = await httpClient.GetFromJsonAsync<RankedMapResponse>("/ranked");

            return response?.List ?? Enumerable.Empty<RankedMap>();
        }

        public string? GetScoreSaberReplayUrl(string mapId, PlayerScore score)
        {
            if (!score.Score.HasReplay)
                return null;

            return $"{_replayBaseUrl}/?id={mapId}&difficulty={score.Leaderboard.Difficulty.DifficultyName}&playerID={_playerId.Value}";
        }
    }
}
