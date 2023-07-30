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
    }
}
