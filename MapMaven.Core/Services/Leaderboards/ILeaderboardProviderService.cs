using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;

namespace MapMaven.Core.Services.Leaderboards
{
    public interface ILeaderboardProviderService
    {
        LeaderboardProvider LeaderboardProviderName { get; }
        string? PlayerId { get; }
        IObservable<string?> PlayerIdObservable { get; }
        IObservable<Dictionary<string, RankedMapInfoItem>> RankedMaps { get; }
        IObservable<PlayerProfile?> PlayerProfile { get; }
        IObservable<IEnumerable<PlayerScore>> PlayerScores { get; }
        IObservable<bool> Active { get; }

        string? GetPlayerIdFromReplays(string beatSaberInstallLocation);
        Task<Dictionary<string, RankedMapInfoItem>> GetRankedMaps();
        string? GetReplayUrl(string mapId, PlayerScore score);
        void ReloadRankedMaps();
        void RefreshPlayerData();
        Task SetPlayerId(string playerId);
    }
}
