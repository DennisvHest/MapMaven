using MapMaven.Core.ApiClients.BeatSaver;
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
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();

        public IEnumerable<RankedMapDifficultyInfo> Difficulties { get; set; } = Enumerable.Empty<RankedMapDifficultyInfo>();

        public RankedMapInfoItem() { }

        public RankedMapInfoItem(ScoreSaberFullRankedMapInfoItem fullRankedMapInfoItem)
        {
            var mapVersion = MapBaseProperties(fullRankedMapInfoItem);

            var leaderboard = fullRankedMapInfoItem.Leaderboards.First();

            Name = leaderboard.SongName;
            SongAuthorName = leaderboard.SongAuthorName;
            MapAuthorName = leaderboard.LevelAuthorName;

            Difficulties = fullRankedMapInfoItem.Leaderboards.GroupJoin(
                mapVersion.Diffs, l => l.Difficulty.DifficultyName, d => d.Difficulty.ToString(),
                (leaderboard, difficulty) => new RankedMapDifficultyInfo(leaderboard, difficulty.First(d => d.Characteristic == "Standard"))
            );
        }

        public RankedMapInfoItem(BeatLeaderFullRankedMapInfoItem fullRankedMapInfoItem)
        {
            var mapVersion = MapBaseProperties(fullRankedMapInfoItem);

            var leaderboard = fullRankedMapInfoItem.Leaderboards.First();

            Name = leaderboard.Song.Name;
            SongAuthorName = leaderboard.Song.Author;
            MapAuthorName = leaderboard.Song.Mapper;

            Difficulties = fullRankedMapInfoItem.Leaderboards.GroupJoin(
                mapVersion.Diffs, l => l.Difficulty.DifficultyName, d => d.Difficulty.ToString(),
                (leaderboard, difficulty) => new RankedMapDifficultyInfo(leaderboard, difficulty.FirstOrDefault(d => d.Characteristic == "Standard") ?? difficulty.First())
            );
        }

        private MapVersion MapBaseProperties(FullRankedMapInfoItem fullRankedMapInfoItem)
        {
            SongHash = fullRankedMapInfoItem.SongHash;
            BeatSaverId = fullRankedMapInfoItem.MapDetail.Id;
            Duration = TimeSpan.FromSeconds(fullRankedMapInfoItem.MapDetail.Metadata.Duration ?? 0);

            var mapVersion = fullRankedMapInfoItem.MapDetail.Versions.FirstOrDefault(x => x.Hash.Equals(SongHash, StringComparison.OrdinalIgnoreCase));

            if (mapVersion is null)
                mapVersion = fullRankedMapInfoItem.MapDetail.Versions.First();

            CoverImageUrl = mapVersion.CoverURL;

            Tags = fullRankedMapInfoItem.MapDetail.Tags;

            return mapVersion;
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
