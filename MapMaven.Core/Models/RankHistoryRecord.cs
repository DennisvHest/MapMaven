using MapMaven.Core.ApiClients.BeatLeader;

namespace MapMaven.Core.Models
{
    public class RankHistoryRecord
    {
        public int? Rank { get; set; }
        public DateOnly Date { get; set; }

        public RankHistoryRecord() { }

        public RankHistoryRecord(PlayerScoreStatsHistory history)
        {
            Rank = history.Rank;
            Date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(history.Timestamp).DateTime);
        }
    }
}
