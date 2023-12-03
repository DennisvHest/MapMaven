using MapMaven.Core.Models.Data.Leaderboards.BeatLeader;
using MapMaven.Core.Models.Data.Leaderboards.ScoreSaber;

namespace MapMaven.Core.Models.Data.Leaderboards
{
    public class LeaderboardData
    {
        public ScoreSaberLeaderboardData ScoreSaber { get; set; }
        public BeatLeaderLeaderboardData BeatLeader { get; set; }
    }
}
