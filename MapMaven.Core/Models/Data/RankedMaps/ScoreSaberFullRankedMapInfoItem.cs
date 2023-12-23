using MapMaven.Core.ApiClients.ScoreSaber;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class ScoreSaberFullRankedMapInfoItem : FullRankedMapInfoItem
    {
        public IEnumerable<LeaderboardInfo> Leaderboards { get; set; } = Enumerable.Empty<LeaderboardInfo>();
    }
}
