namespace BeatSaberTools.Models.Data.Playlists
{
    public class PlaylistSongInfo
    {
        public string SongName { get; set; }
        public string LevelAuthorName { get; set; }
        public string Hash { get; set; }
        public string LevelId { get; set; }

        public ICollection<PlaylistSongDifficultyInfo> Difficulties { get; set; }
    }
}
