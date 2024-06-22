using MapMaven.Core.Extensions;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards.ScoreEstimation;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly IEnumerable<ILeaderboardProviderService> _leaderboardProviders;
        private readonly IEnumerable<IScoreEstimationService> _scoreEstimationServices;
        private readonly IApplicationSettingService _applicationSettingService;

        public Dictionary<LeaderboardProvider?, ILeaderboardProviderService> LeaderboardProviders { get; private set; }
        public Dictionary<LeaderboardProvider?, IScoreEstimationService> ScoreEstimationServices { get; private set; }

        private readonly BehaviorSubject<LeaderboardProvider?> _activeLeaderboardProviderName = new(null);

        public IObservable<string?> PlayerIdObservable { get; private set; }
        public IObservable<PlayerProfile?> PlayerProfile { get; private set; }
        public IObservable<IEnumerable<PlayerScore>> PlayerScores { get; private set; }
        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; private set; }
        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps { get; private set; }
        public IObservable<LeaderboardProvider?> ActiveLeaderboardProviderName => _activeLeaderboardProviderName;
        public IObservable<IEnumerable<ILeaderboardProviderService>> AvailableLeaderboardProviderServices { get; private set; }

        public string? PlayerId => _activeLeaderboardProviderName.Value is not null ? LeaderboardProviders[_activeLeaderboardProviderName.Value].PlayerId : null;
        public LeaderboardProvider? ActiveLeaderboardProviderNameValue => _activeLeaderboardProviderName.Value;

        public const string ReplayBaseUrl = "https://replay.beatleader.xyz";

        public LeaderboardService(
            IEnumerable<ILeaderboardProviderService> leaderboardProviders,
            IEnumerable<IScoreEstimationService> scoreEstimationServices,
            IApplicationSettingService applicationSettingService)
        {
            _leaderboardProviders = leaderboardProviders;
            _scoreEstimationServices = scoreEstimationServices;
            _applicationSettingService = applicationSettingService;

            LeaderboardProviders = _leaderboardProviders.ToDictionary(x => x.LeaderboardProviderName as LeaderboardProvider?);
            ScoreEstimationServices = _scoreEstimationServices.ToDictionary(x => x.LeaderboardProviderName as LeaderboardProvider?);

            var activeLeaderboardProviderSettingObservable = _applicationSettingService.ApplicationSettings
                .Select(x => x.TryGetValue("ActiveLeaderboardProvider", out var value) ? value : null);
            
            var activeLeaderboardProvidersObservable = _leaderboardProviders
                .Select(x => x.Active.Select(active => new
                {
                    Active = active,
                    LeaderboardProvider = x
                }))
                .CombineLatest()
                .Select(x => x.Where(p => p.Active));

            Observable.CombineLatest(
                activeLeaderboardProvidersObservable, activeLeaderboardProviderSettingObservable,
                (activeLeaderboardProviders, activeLeaderboardProviderSetting) => (activeLeaderboardProviders, activeLeaderboardProviderSetting)
            ).SubscribeAsync(async x =>
            {
                LeaderboardProvider? activeLeaderboardProvider = null;

                // If there is only one active leaderboard provider, set it as the active one
                if (x.activeLeaderboardProviders.Count() == 1)
                {
                    activeLeaderboardProvider = x.activeLeaderboardProviders.FirstOrDefault()?.LeaderboardProvider?.LeaderboardProviderName;
                }
                else
                {
                    activeLeaderboardProvider = Enum.TryParse<LeaderboardProvider>(x.activeLeaderboardProviderSetting?.StringValue, out var leaderboardProviderName) ? leaderboardProviderName : null;
                }

                if (activeLeaderboardProvider.HasValue && _activeLeaderboardProviderName.Value != activeLeaderboardProvider)
                    await SetActiveLeaderboardProviderAsync(activeLeaderboardProvider.Value);
            });

            var activeLeaderboardProviderService = _activeLeaderboardProviderName
                .Select(leaderboardProviderName =>
                {
                    if (leaderboardProviderName is null || !LeaderboardProviders.ContainsKey(leaderboardProviderName))
                        return null;

                    return LeaderboardProviders[leaderboardProviderName];
                });

            var leaderboardProviderPlayerIds = _leaderboardProviders.Select(p =>
                p.PlayerIdObservable.Select(playerId => (leaderboardProvider: p, playerId))
            );

            AvailableLeaderboardProviderServices = Observable.CombineLatest(leaderboardProviderPlayerIds, (leaderboardProviders) =>
                leaderboardProviders
                    .Where(x => !string.IsNullOrEmpty(x.playerId))
                    .Select(x => x.leaderboardProvider)
            );

            PlayerIdObservable = activeLeaderboardProviderService
                .Select(x => x?.PlayerIdObservable ?? Observable.Return(null as string))
                .Switch();

            PlayerProfile = activeLeaderboardProviderService
                .Select(x => x?.PlayerProfile ?? Observable.Return(null as PlayerProfile))
                .Switch();

            PlayerScores = activeLeaderboardProviderService
                .Select(x => x?.PlayerScores ?? Observable.Return(Enumerable.Empty<PlayerScore>()))
                .Switch();

            RankedMapScoreEstimates = activeLeaderboardProviderService
                .Select(activeLeaderboardProvider =>
                {
                    if (activeLeaderboardProvider is null)
                        return Observable.Return(Enumerable.Empty<ScoreEstimate>());

                    if (ScoreEstimationServices.TryGetValue(activeLeaderboardProvider?.LeaderboardProviderName, out var activeScoreEstimationService))
                    {
                        return activeScoreEstimationService.RankedMapScoreEstimates;
                    }
                    else
                    {
                        return Observable.Return(Enumerable.Empty<ScoreEstimate>());
                    }
                })
                .Switch();

            RankedMaps = activeLeaderboardProviderService
                .Select(x => x?.RankedMaps ?? Observable.Return(new Dictionary<string, RankedMapInfoItem>()))
                .Switch();
        }

        public async Task SetActiveLeaderboardProviderAsync(LeaderboardProvider leaderboardProviderName)
        {
            await _applicationSettingService.AddOrUpdateAsync("ActiveLeaderboardProvider", leaderboardProviderName.ToString());

            _activeLeaderboardProviderName.OnNext(leaderboardProviderName);
        }

        public async Task SetPlayerId(string playerId)
        {
            await LeaderboardProviders[_activeLeaderboardProviderName.Value].SetPlayerId(playerId);
        }

        public void RefreshPlayerData()
        {
            foreach (var leaderboardProviderService in LeaderboardProviders.Values)
            {
                leaderboardProviderService.RefreshPlayerData();
            }
        }

        public string? GetPlayerIdFromReplays(string beatSaberInstallLocation) => LeaderboardProviders[_activeLeaderboardProviderName.Value].GetPlayerIdFromReplays(beatSaberInstallLocation);

        public void ReloadRankedMaps()
        {
            foreach (var leaderboardProviderService in LeaderboardProviders.Values)
            {
                leaderboardProviderService.ReloadRankedMaps();
            }
        }

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps() => await LeaderboardProviders[_activeLeaderboardProviderName.Value].GetRankedMaps();

        public string? GetReplayUrl(string mapId, PlayerScore score) => LeaderboardProviders[_activeLeaderboardProviderName.Value].GetReplayUrl(mapId, score);
    }
}
