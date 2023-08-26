using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.ApiClients.ScoreSaber;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class FullRankedMapInfoItem
    {
        public string SongHash { get; set; }
        public MapDetail MapDetail { get; set; }
        public IEnumerable<LeaderboardInfo> Leaderboards { get; set; } = Enumerable.Empty<LeaderboardInfo>();
    }
}
