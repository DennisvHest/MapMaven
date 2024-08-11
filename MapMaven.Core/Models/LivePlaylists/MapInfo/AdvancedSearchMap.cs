using MapMaven.Core.Utilities.LivePlaylists;
using MapMaven.Models;
using MapMaven.Utilities.LivePlaylists;
using System.ComponentModel;

namespace MapMaven.Core.Models.LivePlaylists.MapInfo
{
    public class AdvancedSearchMap
    {
        [DisplayName("Map name")]
        public string Name { get; set; }

        [DisplayName("Song author")]
        public string SongAuthorName { get; set; }

        [DisplayName("Map author")]
        public string MapAuthorName { get; set; }

        [DisplayName("Date added")]
        public DateTime? AddedDateTime { get; set; }

        [ApplicableForMapPool(MapPool.Improvement)]
        public bool Hidden { get; set; }

        public bool Played { get; set; }

        [DisplayName("Star difficulty")]
        [ApplicableForMapPool(MapPool.Improvement)]
        public double Stars { get; set; }

        [ApplicableForMapPool(MapPool.Improvement)]
        [HasPredefinedOptions]
        public string Difficulty { get; set; }

        [ApplicableForMapPool(MapPool.Improvement)]
        [HasPredefinedOptions]
        public IEnumerable<string> Tags { get; set; } = [];

        public LivePlaylistScore? Score { get; set; }
        public LivePlaylistScoreEstimate? ScoreEstimate { get; set; }

        public AdvancedSearchMap() { }

        public AdvancedSearchMap(Map map)
        {
            Name = map.Name;
            SongAuthorName = map.SongAuthorName;
            MapAuthorName = map.MapAuthorName;
            AddedDateTime = map.AddedDateTime;
            Hidden = map.Hidden;
            Played = map.Played;
            Stars = map.Difficulty?.Stars ?? 0;
            Difficulty = map.Difficulty?.Difficulty ?? "";
            Tags = map.Tags ?? [];
            Score = map.HighestPlayerScore != null
                ? new LivePlaylistScore(map.HighestPlayerScore)
                : null;
            ScoreEstimate = map.ScoreEstimate != null
                ? new LivePlaylistScoreEstimate(map.ScoreEstimate)
                : null;
        }
    }
}
