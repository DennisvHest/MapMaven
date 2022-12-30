using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Models.Data.ScoreSaber;
using BeatSaberTools.Core.Utilities.Scoresaber;
using BeatSaberTools_Core;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class ScoreSaberService
    {
        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly IBeatSaverFileService _fileService;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BehaviorSubject<string?> _playerId = new(null);
        private readonly BehaviorSubject<IEnumerable<RankedMap>> _rankedMaps = new(Enumerable.Empty<RankedMap>());

        public readonly IObservable<Player> PlayerProfile;
        public readonly IObservable<IEnumerable<PlayerScore>> PlayerScores;
        public readonly IObservable<IEnumerable<ScoreEstimate>> ScoreEstimates;
        public readonly IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates;

        public IObservable<IEnumerable<RankedMap>> RankedMaps => _rankedMaps;

        public string? PlayerId => _playerId.Value;

        private const string _replayBaseUrl = "https://www.replay.beatleader.xyz";

        public ScoreSaberService(
            ScoreSaberApiClient scoreSaber,
            IBeatSaverFileService fileService,
            IHttpClientFactory httpClientFactory)
        {
            _scoreSaber = scoreSaber;
            _fileService = fileService;
            _httpClientFactory = httpClientFactory;

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
                        var scoreCollection = await _scoreSaber.Scores2Async(
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

            PlayerScores = playerScores;

            PlayerProfile = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    return Observable.Return(null as Player);

                return Observable.FromAsync(async () => await _scoreSaber.FullAsync(playerId));
            }).Concat();

            var scoreEstimates = Observable.CombineLatest(PlayerProfile, PlayerScores, RankedMaps, (player, playerScores, rankedMaps) =>
            {
                if (player == null)
                    return Enumerable.Empty<ScoreEstimate>();

                var rankedMapPlayerScorePairs = playerScores
                    .Join(rankedMaps, playerScore => playerScore.Leaderboard.SongHash + playerScore.Leaderboard.Difficulty.DifficultyName.ToLower(), rankedMap => rankedMap.Id + rankedMap.Difficulty.ToLower(), (playerScore, rankedMap) =>
                    {
                        return new RankedMapScorePair
                        {
                            Map = rankedMap,
                            PlayerScore = playerScore
                        };
                    });

                var scoresaber = new Scoresaber_old(player, rankedMapPlayerScorePairs.Select(x => x.PlayerScore));

                return rankedMapPlayerScorePairs.Select(pair =>
                {
                    var output = ScoreEstimateMLModel.Predict(new ScoreEstimateMLModel.ModelInput
                    {
                        PP = Convert.ToSingle(player.Pp),
                        StarDifficulty = Convert.ToSingle(pair.Map.Stars),
                        TimeSet = DateTime.Now
                    });

                    return scoresaber.GetScoreEstimate(pair.Map, Convert.ToDecimal(output.Score));
                }).ToList();
            }).Replay(1);

            scoreEstimates.Connect();

            ScoreEstimates = scoreEstimates;

            var rankedMapScoreEstimates = Observable.CombineLatest(PlayerProfile, PlayerScores, RankedMaps, (player, playerScores, rankedMaps) =>
            {
                if (player == null)
                    return Enumerable.Empty<ScoreEstimate>();

                var scoresaber = new Scoresaber_old(player, playerScores);

                return rankedMaps.Select(map =>
                {
                    var output = ScoreEstimateMLModel.Predict(new ScoreEstimateMLModel.ModelInput
                    {
                        PP = Convert.ToSingle(player.Pp),
                        StarDifficulty = Convert.ToSingle(map.Stars),
                        TimeSet = DateTime.Now
                    });

                    return scoresaber.GetScoreEstimate(map, Convert.ToDecimal(output.Score));
                }).ToList();
            }).Replay(1);

            rankedMapScoreEstimates.Connect();

            RankedMapScoreEstimates = rankedMapScoreEstimates;
        }

        public async Task LoadPlayerData()
        {
            var scoreSaberReplaysLocation = Path.Combine(_fileService.UserDataLocation, "ScoreSaber", "Replays");

            if (!Directory.Exists(scoreSaberReplaysLocation))
                return;

            var replayFileName = Directory.EnumerateFiles(scoreSaberReplaysLocation, "*.dat").FirstOrDefault();

            if (string.IsNullOrEmpty(replayFileName))
                return;

            var replayFile = new FileInfo(replayFileName);

            var playerId = replayFile.Name
                .Split('-')
                .First();

            _playerId.OnNext(playerId);
        }

        public async Task LoadRankedMaps()
        {
            var rankedMaps = await GetRankedMaps();

            _rankedMaps.OnNext(rankedMaps);
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
