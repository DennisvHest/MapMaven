using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Services.Leaderboards
{
    public interface ILeaderboardService
    {
        string? PlayerId { get; }
        IObservable<string?> PlayerIdObservable { get; }
        IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps { get; }
        IObservable<PlayerProfile?> PlayerProfile { get; }
        IObservable<IEnumerable<Models.PlayerScore>> PlayerScores { get; }
        IObservable<IEnumerable<ScoreEstimate>> RankedMapScoreEstimates { get; }
        IObservable<LeaderboardProvider?> ActiveLeaderboardProviderName { get; }
        Dictionary<LeaderboardProvider?, ILeaderboardProviderService> LeaderboardProviders { get; }

        string? GetPlayerIdFromReplays(string beatSaberInstallLocation);
        Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps();
        string? GetReplayUrl(string mapId, Models.PlayerScore score);
        Task LoadRankedMaps();
        void RefreshPlayerData();
        void SetActiveLeaderboardProvider(LeaderboardProvider leaderboardProviderName);
        Task SetPlayerId(string playerId);
    }
}