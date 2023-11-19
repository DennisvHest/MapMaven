using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities.Scoresaber;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Leaderboards
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly IEnumerable<ILeaderboardProvider> _leaderboardProviders;

        private readonly IApplicationSettingService _applicationSettingService;

        public Dictionary<string, ILeaderboardProvider> LeaderboardProviders { get; private set; }

        private readonly BehaviorSubject<string?> _activeLeaderboardProviderName = new(Models.LeaderboardProviders.BeatLeader);

        public IObservable<string?> PlayerIdObservable { get; private set; }
        public IObservable<PlayerProfile?> PlayerProfile { get; private set; }
        public IObservable<IEnumerable<PlayerScore>> PlayerScores { get; private set; }
        public IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; private set; }
        public IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps { get; private set; }
        public IObservable<string?> ActiveLeaderboardProviderName => _activeLeaderboardProviderName;

        public string? PlayerId => LeaderboardProviders[_activeLeaderboardProviderName.Value].PlayerId;

        public const string ReplayBaseUrl = "https://www.replay.beatleader.xyz";

        public LeaderboardService(
            IEnumerable<ILeaderboardProvider> leaderboardProviders,
            IApplicationSettingService applicationSettingService)
        {
            _leaderboardProviders = leaderboardProviders;
            _applicationSettingService = applicationSettingService;

            LeaderboardProviders = _leaderboardProviders.ToDictionary(x => x.LeaderboardProviderName);

            // If there is only one leaderboard provider, set it as the active one
            _leaderboardProviders
                .Select(x => x.PlayerIdObservable.StartWith(null as string))
                .CombineLatest()
                .Subscribe(playerIds =>
                {
                    if (playerIds.Where(x => !string.IsNullOrEmpty(x)).Count() > 1)
                        return;

                    var activeLeaderboardProvider = _leaderboardProviders.FirstOrDefault(p => !string.IsNullOrEmpty(p.PlayerId));

                    if (activeLeaderboardProvider == null)
                        return;

                    SetActiveLeaderboardProvider(activeLeaderboardProvider.LeaderboardProviderName);
                });

            var activeLeaderboardProviderService = _activeLeaderboardProviderName
                .Select(leaderboardProviderName =>
                {
                    if (!LeaderboardProviders.ContainsKey(leaderboardProviderName))
                        return null;

                    return LeaderboardProviders[leaderboardProviderName];
                });

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
                .Select(x => x?.RankedMapScoreEstimates ?? Observable.Return(Enumerable.Empty<ScoreEstimate>()))
                .Switch();

            RankedMaps = activeLeaderboardProviderService
                .Select(x => x?.RankedMaps ?? Observable.Return(new Dictionary<string, RankedMapInfoItem>()))
                .Switch();
        }

        public void SetActiveLeaderboardProvider(string leaderboardProviderName)
        {
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

        public async Task LoadRankedMaps()
        {
            await Task.WhenAll(
                LeaderboardProviders.Values.Select(x => x.LoadRankedMaps())
            );
        }

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps() => await LeaderboardProviders[_activeLeaderboardProviderName.Value].GetRankedMaps();

        public string? GetReplayUrl(string mapId, PlayerScore score) => LeaderboardProviders[_activeLeaderboardProviderName.Value].GetReplayUrl(mapId, score);
    }
}
