using MapMaven.Core.Utilities.Scoresaber;
using MapMaven.Models;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class RankedMapInfoItem
    {
        public string SongHash { get; set; }
        public string BeatSaverId { get; set; }
        public string Name { get; set; }
        public string SongAuthorName { get; set; }
        public string MapAuthorName { get; set; }
        public TimeSpan Duration { get; set; }
        public string CoverImageUrl { get; set; }

        public IEnumerable<RankedMapDifficultyInfo> Difficulties { get; set; } = Enumerable.Empty<RankedMapDifficultyInfo>();

        public RankedMapInfoItem() { }

        public RankedMapInfoItem(FullRankedMapInfoItem fullRankedMapInfoItem)
        {
            var leaderboard = fullRankedMapInfoItem.Leaderboards.First();

            SongHash = fullRankedMapInfoItem.SongHash;
            BeatSaverId = fullRankedMapInfoItem.MapDetail.Id;
            Name = leaderboard.SongName;
            SongAuthorName = leaderboard.SongAuthorName;
            MapAuthorName = leaderboard.LevelAuthorName;
            Duration = TimeSpan.FromSeconds(fullRankedMapInfoItem.MapDetail.Metadata.Duration ?? 0);

            var mapVersion = fullRankedMapInfoItem.MapDetail.Versions.First(x => x.Hash.Equals(SongHash, StringComparison.OrdinalIgnoreCase));

            CoverImageUrl = mapVersion.CoverURL;

            Difficulties = fullRankedMapInfoItem.Leaderboards.Select(x => new RankedMapDifficultyInfo
            {
                Stars = x.Stars,
                MaxPP = x.Stars * Scoresaber.PPPerStar,
                Difficulty = x.Difficulty.DifficultyName
            });
        }

        public Map ToMap()
        {
            return new Map
            {
                Id = BeatSaverId,
                Hash = SongHash,
                Name = Name,
                SongAuthorName = SongAuthorName,
                MapAuthorName = MapAuthorName,
                SongDuration = Duration,
                CoverImageUrl = CoverImageUrl
            };
        }
    }
}
