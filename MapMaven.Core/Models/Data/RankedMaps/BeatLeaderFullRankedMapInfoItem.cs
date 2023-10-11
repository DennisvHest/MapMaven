
using MapMaven.Core.ApiClients.BeatLeader;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class BeatLeaderFullRankedMapInfoItem : FullRankedMapInfoItem
    {
        public IEnumerable<LeaderboardInfoResponse> Leaderboards { get; set; } = Enumerable.Empty<LeaderboardInfoResponse>();
    }
}
