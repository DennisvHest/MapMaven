using MapMaven.Models;
using MapMaven.Utilities.DynamicPlaylists;
using System.ComponentModel;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistMap
    {
        [DisplayName("Map name")]
        public string Name { get; set; }

        [DisplayName("Song author")]
        public string SongAuthorName { get; set; }

        [DisplayName("Map author")]
        public string MapAuthorName { get; set; }

        [DisplayName("Date added")]
        public DateTime AddedDateTime { get; set; }

        [ApplicableForMapPool(MapPool.Improvement)]
        public bool Hidden { get; set; }

        public bool Played { get; set; }

        [DisplayName("Star difficulty")]
        [ApplicableForMapPool(MapPool.Improvement)]
        public double Stars { get; set; }

        [ApplicableForMapPool(MapPool.Improvement)]
        public string Difficulty { get; set; }

        public DynamicPlaylistScore? Score { get; set; }
        public DynamicPlaylistScoreEstimate? ScoreEstimate { get; set; }

        public DynamicPlaylistMap() { }

        public DynamicPlaylistMap(Map map)
        {
            Name = map.Name;
            SongAuthorName = map.SongAuthorName;
            MapAuthorName = map.MapAuthorName;
            AddedDateTime = map.AddedDateTime;
            Hidden = map.Hidden;
            Played = map.Played;
            Stars = map.Difficulty?.Stars ?? 0;
            Difficulty = map.Difficulty?.Difficulty ?? "";
            Score = map.HighestPlayerScore != null
                ? new DynamicPlaylistScore(map.HighestPlayerScore)
                : null;
            ScoreEstimate = map.ScoreEstimate != null
                ? new DynamicPlaylistScoreEstimate(map.ScoreEstimate)
                : null;
        }
    }
}
