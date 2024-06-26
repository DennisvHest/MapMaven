﻿using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
using System.IO.Abstractions;
using MapMaven.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards
{
    public class ScoreSaberService : ILeaderboardProviderService
    {
        public LeaderboardProvider LeaderboardProviderName => LeaderboardProvider.ScoreSaber;

        private readonly ScoreSaberApiClient _scoreSaber;
        private readonly IApplicationSettingService _applicationSettingService;
        private readonly IApplicationEventService _applicationEventService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileSystem _fileSystem;

        private readonly ILogger<ScoreSaberService> _logger;

        private readonly BehaviorSubject<string?> _playerId = new(null);

        private readonly CachedValue<Dictionary<string, RankedMapInfoItem>> _rankedMaps;

        public IObservable<string?> PlayerIdObservable => _playerId;
        public IObservable<PlayerProfile?> PlayerProfile { get; private set; }
        public IObservable<IEnumerable<Models.PlayerScore>> PlayerScores { get; private set; }

        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps => _rankedMaps.ValueObservable;

        public IObservable<bool> Active { get; private set; }

        public string? PlayerId => _playerId.Value;

        public const string PlayerIdSettingKey = "PlayerId";

        public ScoreSaberService(
            ScoreSaberApiClient scoreSaber,
            IHttpClientFactory httpClientFactory,
            IApplicationSettingService applicationSettingService,
            IApplicationEventService applicationEventService,
            IFileSystem fileSystem,
            ILogger<ScoreSaberService> logger)
        {
            _scoreSaber = scoreSaber;
            _httpClientFactory = httpClientFactory;
            _applicationSettingService = applicationSettingService;
            _applicationEventService = applicationEventService;
            _fileSystem = fileSystem;
            _logger = logger;

            var playerScores = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    return Observable.Return(Enumerable.Empty<Models.PlayerScore>());

                return Observable.FromAsync(async () =>
                {
                    var playerScores = Enumerable.Empty<Models.PlayerScore>();
                    int totalScores;
                    int page = 1;

                    do
                    {
                        PlayerScoreCollection scoreCollection;

                        try
                        {
                            scoreCollection = await _scoreSaber.ScoresAsync(
                                playerId: playerId,
                                limit: 100,
                                sort: Sort.Top,
                                page: page,
                                withMetadata: true
                            );
                        }
                        catch (ScoreSaberApiClient.ApiException exception)
                        {
                            // The ScoreSaber API returns 404 when player has no scores, so just return an empty list of PlayerScores.
                            if (exception.StatusCode == 404)
                            {
                                break;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        totalScores = scoreCollection.Metadata.Total;

                        playerScores = playerScores.Concat(scoreCollection.PlayerScores.Select(s => new Models.PlayerScore(s)));

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

                    return Observable.Return(Enumerable.Empty<Models.PlayerScore>());
                });
            }).Concat().Replay(1);

            playerScores.Connect();

            PlayerScores = playerScores;

            PlayerProfile = _playerId.Select(playerId =>
            {
                if (string.IsNullOrEmpty(playerId))
                    return Observable.Return(null as PlayerProfile);

                return Observable.FromAsync(async () =>
                {
                    var playerProfile = await _scoreSaber.FullAsync(playerId);

                    return new PlayerProfile(playerProfile);
                })
                .Catch((Exception exception) =>
                {
                    _applicationEventService.RaiseError(new ErrorEvent
                    {
                        Exception = exception,
                        Message = "Failed to load player profile from ScoreSaber."
                    });

                    return Observable.Return(null as PlayerProfile);
                });
            }).Concat();

            _rankedMaps = new(GetRankedMaps, TimeSpan.FromHours(6), []);

            var playerId = _applicationSettingService.ApplicationSettings
                .Select(applicationSettings => applicationSettings.TryGetValue(PlayerIdSettingKey, out var playerId) ? playerId.StringValue : null)
                .DistinctUntilChanged();

            playerId.Subscribe(_playerId.OnNext);

            Active = playerId.Select(playerId => !string.IsNullOrEmpty(playerId));
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

            if (!_fileSystem.Directory.Exists(scoreSaberReplaysLocation))
                return null;

            var replayFileName = _fileSystem.Directory.EnumerateFiles(scoreSaberReplaysLocation, "*.dat").FirstOrDefault();

            if (string.IsNullOrEmpty(replayFileName))
                return null;

            var replayFile = new FileInfo(replayFileName);

            var playerId = replayFile.Name
                .Split('-')
                .First();

            return playerId;
        }

        public void ReloadRankedMaps()
        {
            _rankedMaps.UpdateValue();
        }

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps()
        {
            try
            {
                _logger.LogInformation("Loading ranked maps for ScoreSaber.");

                var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

                var response = await httpClient.GetFromJsonAsync<RankedMapInfo>("/scoresaber/ranked-maps.json");

                return response?.RankedMaps ?? [];
            }
            catch (Exception ex)
            {
                _applicationEventService.RaiseError(new()
                {
                    Exception = ex,
                    Message = "Failed to load ranked maps from ScoreSaber."
                });

                return _rankedMaps.Value;
            }
        }

        public string? GetReplayUrl(string mapId, Models.PlayerScore score)
        {
            if (!score.Score.HasReplay)
                return null;

            return $"{LeaderboardService.ReplayBaseUrl}/?id={mapId}&difficulty={score.Leaderboard.Difficulty}&playerID={_playerId.Value}";
        }
    }
}
