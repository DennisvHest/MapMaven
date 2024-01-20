using MapMaven.Core.Models.Data.Leaderboards;

namespace MapMaven.Core.Services.Leaderboards
{
    public interface ILeaderboardDataService
    {
        IObservable<LeaderboardData?> LeaderboardData { get; }

        Task<LeaderboardData?> GetLeaderboardDataAsync();
        void ReloadLeaderboardData();
    }
}