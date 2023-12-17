using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Utilities.Scoresaber;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using static MapMaven_Core.ScoreSaberScoreEstimateMLModel;

namespace MapMaven.Core.Services.Leaderboards.ScoreEstimation
{
    public class ScoreSaberScoreEstimationService : IScoreEstimationService
    {
        public LeaderboardProvider LeaderboardProviderName => LeaderboardProvider.ScoreSaber;

        private readonly LeaderboardDataService _leaderboardDataService;
        private readonly ScoreSaberService _scoreSaberService;
        private readonly ScoreEstimationSettings _scoreEstimationSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        private PredictionEngine<ModelInput, ModelOutput> _predictEngine;

        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; private set; }

        private readonly BehaviorSubject<bool> _estimatingScores = new(false);
        public IObservable<bool> EstimatingScores => _estimatingScores;


        private readonly ILogger<ScoreSaberScoreEstimationService> _logger;

        private const string _scoreEstimationModelFileName = "ScoreSaberScoreEstimateMLModel.mlnet";

        private static string _modelPath
        {
            get
            {
                var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                return Path.Join(currentDirectory, "ScoreEstimation", _scoreEstimationModelFileName);
            }
        }

        public ScoreSaberScoreEstimationService(
            LeaderboardDataService leaderboardDataService,
            ScoreSaberService scoreSaberService,
            ScoreEstimationSettings scoreEstimationSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<ScoreSaberScoreEstimationService> logger)
        {
            _leaderboardDataService = leaderboardDataService;
            _scoreSaberService = scoreSaberService;
            _scoreEstimationSettings = scoreEstimationSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            var rankedMapScoreEstimates = Observable.CombineLatest(
                _scoreSaberService.PlayerProfile,
                _scoreSaberService.PlayerScores,
                _scoreSaberService.RankedMaps,
                _leaderboardDataService.LeaderboardData,
                _scoreEstimationSettings.DifficultyModifierValue.Throttle(TimeSpan.FromSeconds(1)),
                GetEstimationsAsync).Switch().Replay(1);

            rankedMapScoreEstimates.Connect();

            RankedMapScoreEstimates = rankedMapScoreEstimates;
        }

        private async Task<IEnumerable<ScoreEstimate>> GetEstimationsAsync(PlayerProfile? player, IEnumerable<PlayerScore> playerScores, Dictionary<string, RankedMapInfoItem> rankedMaps, LeaderboardData? leaderboardData, int difficultyAmplifierValue)
        {
            _estimatingScores.OnNext(true);

            if (_predictEngine is null)
                await InitializeEstimationEngine();

            if (player == null || leaderboardData?.ScoreSaber == null)
                return Enumerable.Empty<ScoreEstimate>();

            var scoresaber = new Scoresaber(player, playerScores, leaderboardData.ScoreSaber);

            var difficultyFactor = difficultyAmplifierValue / 100D;

            var estimates = rankedMaps.SelectMany(map => map.Value.Difficulties.Select(difficulty =>
            {
                var output = Predict(new()
                {
                    PP = Convert.ToSingle(player.Pp * (1 + difficultyFactor)),
                    StarDifficulty = Convert.ToSingle(difficulty.Stars),
                    TimeSet = DateTime.Now
                });

                return scoresaber.GetScoreEstimate(map.Value, difficulty, output.Score);
            })).ToList();

            _estimatingScores.OnNext(false);

            return estimates;
        }

        private ModelOutput Predict(ModelInput input) => _predictEngine.Predict(input);

        private async Task InitializeEstimationEngine()
        {
            var mlContext = new MLContext();

            var modelStream = await GetModel();
            var mlModel = mlContext.Model.Load(modelStream, out var _);

            _predictEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
        }

        private async Task<Stream> GetModel()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

                var modelStream = await httpClient.GetStreamAsync($"scoresaber/models/{_scoreEstimationModelFileName}");

                using (var fileStream = File.OpenWrite(_modelPath))
                {
                    await modelStream.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load score estimation model from server. Falling back to local cache.");
            }

            return File.OpenRead(_modelPath);
        }
    }
}
