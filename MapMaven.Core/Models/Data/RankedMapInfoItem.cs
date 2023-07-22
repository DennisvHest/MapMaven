using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.ApiClients.ScoreSaber;

namespace MapMaven.Core.Models.Data
{
    public class RankedMapInfoItem
    {
        public LeaderboardInfo Leaderboard { get; set; }
        public MapDetail MapDetail { get; set; }
    }
}
