using MapMaven.Core.ApiClients.ScoreSaber;
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

        public string? PlayerId => LeaderboardProviders[_activeLeaderboardProviderName.Value].PlayerId;

        private const string PlayerIdSettingKey = "PlayerId";

        public LeaderboardService(
            IEnumerable<ILeaderboardProvider> leaderboardProviders,
            IApplicationSettingService applicationSettingService)
        {
            _leaderboardProviders = leaderboardProviders;
            _applicationSettingService = applicationSettingService;

            LeaderboardProviders = _leaderboardProviders.ToDictionary(x => x.LeaderboardProviderName);

            var activeLeaderboardProviderService = _activeLeaderboardProviderName
                .Select(leaderboardProviderName =>
                {
                    if (!LeaderboardProviders.ContainsKey(leaderboardProviderName))
                        return null;

                    return LeaderboardProviders[leaderboardProviderName];
                });

            PlayerIdObservable = activeLeaderboardProviderService
                .Select(x => x?.PlayerIdObservable ?? Observable.Return(null as string))
                .Concat();

            PlayerProfile = activeLeaderboardProviderService
                .Select(x => x?.PlayerProfile ?? Observable.Return(null as PlayerProfile))
                .Concat();

            PlayerScores = activeLeaderboardProviderService
                .Select(x => x?.PlayerScores ?? Observable.Return(Enumerable.Empty<PlayerScore>()))
                .Concat();

            RankedMapScoreEstimates = activeLeaderboardProviderService
                .Select(x => x?.RankedMapScoreEstimates ?? Observable.Return(Enumerable.Empty<ScoreEstimate>()))
                .Concat();

            RankedMaps = activeLeaderboardProviderService
                .Select(x => x?.RankedMaps ?? Observable.Return(new Dictionary<string, RankedMapInfoItem>()))
                .Concat();
        }

        public async Task SetPlayerId(string playerId)
        {
            await _applicationSettingService.AddOrUpdateAsync(PlayerIdSettingKey, playerId);
        }

        public void RefreshPlayerData()
        {
            foreach (var leaderboardProviderService in LeaderboardProviders.Values)
            {
                leaderboardProviderService.RefreshPlayerData();
            }
        }

        public string? GetPlayerIdFromReplays(string beatSaberInstallLocation) => LeaderboardProviders[_activeLeaderboardProviderName.Value].GetPlayerIdFromReplays(beatSaberInstallLocation);

        public async Task LoadRankedMaps() => await LeaderboardProviders[_activeLeaderboardProviderName.Value].LoadRankedMaps();

        public async Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps() => await LeaderboardProviders[_activeLeaderboardProviderName.Value].GetRankedMaps();

        public string? GetReplayUrl(string mapId, PlayerScore score) => LeaderboardProviders[_activeLeaderboardProviderName.Value].GetReplayUrl(mapId, score);
    }
}
