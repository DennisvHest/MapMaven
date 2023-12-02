using MapMaven.Core.Models;

namespace MapMaven.Utility
{
    public static class LeaderboardUtility
    {
        public static string GetLogoPath(LeaderboardProvider leaderboardProvider)
        {
            return leaderboardProvider switch
            {
                LeaderboardProvider.ScoreSaber => "/images/score-saber-logo-small.png",
                LeaderboardProvider.BeatLeader => "/images/beat-leader-logo.svg",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
