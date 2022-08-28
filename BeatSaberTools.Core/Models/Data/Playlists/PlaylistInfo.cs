namespace BeatSaberTools.Models.Data.Playlists
{
    public class PlaylistInfo
    {
        public string PlaylistTitle { get; set; }
        public string PlaylistAuthor { get; set; }
        public string Image { get; set; }

        public ICollection<PlaylistSongInfo> Songs { get; set; }
    }
}
