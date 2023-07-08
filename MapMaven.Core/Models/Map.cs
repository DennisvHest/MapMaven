using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Models
{
    public class Map
    {
        public string Id { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }
        public string SongAuthorName { get; set; }
        public string MapAuthorName { get; set; }
        public DateTime AddedDateTime { get; set; }
        public TimeSpan SongDuration { get; set; }
        public TimeSpan PreviewStartTime { get; set; }
        public TimeSpan PreviewDuration { get; set; }
        public TimeSpan PreviewEndTime => PreviewStartTime + PreviewDuration;
        public string CoverImageUrl { get; set; }
        public bool Hidden { get; set; }
        public bool Played => HighestPlayerScore != null;

        public PlayerScore? HighestPlayerScore { get; set; }
        public IEnumerable<PlayerScore> AllPlayerScores { get; set; } = Enumerable.Empty<PlayerScore>();
        public RankedMap? RankedMap { get; set; }
        public IEnumerable<ScoreEstimate> ScoreEstimates { get; set; } = Enumerable.Empty<ScoreEstimate>();
        public ScoreEstimate? ScoreEstimate => ScoreEstimates.FirstOrDefault();

    }
}
