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
        public decimal Stars { get; set; }
        public string Difficulty { get; set; }
        public decimal Pp { get; set; }
        public DynamicPlaylistScore Score { get; set; }
        public DynamicPlaylistScoreEstimate ScoreEstimate { get; set; }
    }
}
