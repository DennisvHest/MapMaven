using BeatSaverSharp.Models;
using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data.RankedMaps;
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
        public bool Ranked => RankedMap != null;

        public PlayerScore? HighestPlayerScore { get; set; }
        public IEnumerable<PlayerScore> AllPlayerScores { get; set; } = Enumerable.Empty<PlayerScore>();
        public RankedMapInfoItem RankedMap { get; private set; }
        public IEnumerable<Core.Models.MapDifficulty> Difficulties { get; set; } = Enumerable.Empty<Core.Models.MapDifficulty>();
        public RankedMapDifficultyInfo? Difficulty { get; set; }
        public IEnumerable<ScoreEstimate> ScoreEstimates { get; set; } = Enumerable.Empty<ScoreEstimate>();
        public ScoreEstimate? ScoreEstimate => ScoreEstimates.FirstOrDefault();

        public void SetRankedMapDetails(RankedMapInfoItem rankedMap)
        {
            if (rankedMap == null)
                return;

            RankedMap = rankedMap;
            Difficulties = rankedMap.Difficulties.Select(d => new Core.Models.MapDifficulty
            {
                Stars = d.Stars,
                MaxPP = d.MaxPP,
                Difficulty = d.Difficulty
            });
        }

        public void SetMapDetails(Beatmap beatmap)
        {
            if (Difficulties.Any() || beatmap.LatestVersion == null)
                return;

            Difficulties = beatmap.LatestVersion.Difficulties.Select(d => new Core.Models.MapDifficulty
            {
                Stars = null,
                MaxPP = null,
                Difficulty = d.Difficulty.ToString()
            });
        }
    }
}
