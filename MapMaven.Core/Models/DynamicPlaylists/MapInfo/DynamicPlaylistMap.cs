using MapMaven.Models;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistMap
    {
        public string Name { get; set; }
        public string SongAuthorName { get; set; }
        public string MapAuthorName { get; set; }
        public DateTime AddedDateTime { get; set; }
        public bool Hidden { get; set; }
        public bool Played { get; set; }
        public double Stars { get; set; }
        public string Difficulty { get; set; }
        public double Pp { get; set; }
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
            Stars = map.RankedMap?.Stars ?? 0;
            Difficulty = map.RankedMap?.Difficulty ?? "";
            Pp = map.ScoreEstimate?.Pp ?? 0;
            Score = map.PlayerScore != null
                ? new DynamicPlaylistScore(map.PlayerScore)
                : null;
            ScoreEstimate = map.ScoreEstimate != null
                ? new DynamicPlaylistScoreEstimate(map.ScoreEstimate)
                : null;
        }
    }
}
