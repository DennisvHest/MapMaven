using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities.Scoresaber;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards
{
    public class BeatLeaderService : ILeaderboardProvider
    {
        private readonly BeatLeaderApiClient _beatLeader;
        private readonly IApplicationSettingService _applicationSettingService;
        private readonly IApplicationEventService _applicationEventService;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BehaviorSubject<string?> _playerId = new(null);
        private readonly BehaviorSubject<Dictionary<string, RankedMapInfoItem>> _rankedMaps = new(new());

        public string LeaderboardProviderName => LeaderboardProviders.BeatLeader;

        public string? PlayerId => _playerId.Value;

        public IObservable<string?> PlayerIdObservable => _playerId;

        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps => _rankedMaps;

        public IObservable<PlayerProfile?> PlayerProfile { get; private set; }

        public IObservable<IEnumerable<PlayerScore>> PlayerScores => Observable.Return(Enumerable.Empty<PlayerScore>());

        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates => Observable.Return(Enumerable.Empty<ScoreEstimate>());

        private const string PlayerIdSettingKey = "BeatLeaderPlayerId";

        public BeatLeaderService(
            BeatLeaderApiClient beatLeader,
            IApplicationSettingService applicationSettingService,
            IApplicationEventService applicationEventService,
            IHttpClientFactory httpClientFactory)
        {
            _beatLeader = beatLeader;
            _applicationSettingService = applicationSettingService;
            _applicationEventService = applicationEventService;
            _httpClientFactory = httpClientFactory;

            PlayerProfile = _playerId
                .Select(playerId =>
                {
                    if (string.IsNullOrEmpty(playerId))
                        return Observable.Return(null as PlayerProfile);

                    return Observable.FromAsync(async () =>
                    {
                        var playerProfile = await _beatLeader.PlayerAsync(
                            id: playerId,
                            stats: false,
                            keepOriginalId: false,
                            leaderboardContext: (LeaderboardContexts)Models.Data.BeatLeader.LeaderboardContexts.General
                        );
                        return new PlayerProfile(playerProfile);
                    });
                })
                .Concat();

            _applicationSettingService.ApplicationSettings
                .Select(applicationSettings => applicationSettings.TryGetValue(PlayerIdSettingKey, out var playerId) ? playerId.StringValue : null)
                .DistinctUntilChanged()
                .Subscribe(_playerId.OnNext);
        }

        public string? GetPlayerIdFromReplays(string beatSaberInstallLocation)
        {
            throw new NotImplementedException();
        }

        public string? GetReplayUrl(string mapId, PlayerScore score)
        {
            throw new NotImplementedException();
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
