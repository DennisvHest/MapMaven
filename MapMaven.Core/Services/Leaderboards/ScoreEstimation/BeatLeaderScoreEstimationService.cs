﻿using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Utilities.BeatLeader;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using static MapMaven_Core.BeatLeaderScoreEstimateMLModel;

namespace MapMaven.Core.Services.Leaderboards.ScoreEstimation
{
    public class BeatLeaderScoreEstimationService : IScoreEstimationService
    {
        public LeaderboardProvider LeaderboardProviderName => LeaderboardProvider.BeatLeader;

        private readonly ILeaderboardDataService _leaderboardDataService;
        private readonly BeatLeaderService _beatLeaderService;
        private readonly ScoreEstimationSettings _scoreEstimationSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileSystem _fileSystem;

        private PredictionEngine<ModelInput, ModelOutput> _predictEngine;
        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; private set; }

        private readonly BehaviorSubject<bool> _estimatingScores = new(false);
        public IObservable<bool> EstimatingScores => _estimatingScores;

        private readonly ILogger<BeatLeaderScoreEstimationService> _logger;

        private const string _scoreEstimationModelFileName = "BeatLeaderScoreEstimateMLModel.mlnet";

        public BeatLeaderScoreEstimationService(
            ILeaderboardDataService leaderboardDataService,
            BeatLeaderService beatLeaderService,
            ScoreEstimationSettings scoreEstimationSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<BeatLeaderScoreEstimationService> logger,
            IFileSystem fileSystem)
        {
            _leaderboardDataService = leaderboardDataService;
            _beatLeaderService = beatLeaderService;
            _scoreEstimationSettings = scoreEstimationSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _fileSystem = fileSystem;

            var rankedMapScoreEstimates = Observable.CombineLatest(
                _beatLeaderService.PlayerProfile,
                _beatLeaderService.PlayerScores,
                _beatLeaderService.RankedMaps,
                _leaderboardDataService.LeaderboardData,
                _scoreEstimationSettings.DifficultyModifierValue.Throttle(TimeSpan.FromSeconds(1)),
                GetEstimationsAsync).Switch().Replay(1);

            rankedMapScoreEstimates.Connect();

            RankedMapScoreEstimates = rankedMapScoreEstimates;
        }

        private string _modelPath
        {
            get
            {
                var currentDirectory = _fileSystem.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                return _fileSystem.Path.Join(currentDirectory, "ScoreEstimation", _scoreEstimationModelFileName);
            }
        }

        private async Task<IEnumerable<ScoreEstimate>> GetEstimationsAsync(PlayerProfile? player, IEnumerable<PlayerScore> playerScores, Dictionary<string, RankedMapInfoItem> rankedMaps, LeaderboardData? leaderboardData, int difficultyModifierValue)
        {
            _estimatingScores.OnNext(true);

            if (_predictEngine is null)
                await InitializeEstimationEngine();

            var estimates = Enumerable.Empty<ScoreEstimate>();

            if (player != null && leaderboardData?.ScoreSaber != null)
            {
                var scoresaber = new BeatLeader(player, playerScores, leaderboardData.BeatLeader);

                var difficultyFactor = difficultyModifierValue / 100D;

                estimates = rankedMaps.SelectMany(map => map.Value.Difficulties.Select(difficulty =>
                {
                    var output = Predict(new()
                    {
                        Pp = Convert.ToSingle(player.Pp * (1 + difficultyFactor)),
                        StarDifficulty = Convert.ToSingle(difficulty.Stars),
                        TimeSet = DateTime.Now
                    });

                    return scoresaber.GetScoreEstimate(map.Value, difficulty, output.Score);
                })).ToList();
            }

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

                var modelStream = await httpClient.GetStreamAsync($"beatleader/models/{_scoreEstimationModelFileName}");

                using (var fileStream = _fileSystem.File.OpenWrite(_modelPath))
                {
                    await modelStream.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load score estimation model from server. Falling back to local cache.");
            }

            return _fileSystem.File.OpenRead(_modelPath);
        }
    }
}
