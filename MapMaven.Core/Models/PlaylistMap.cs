using BeatSaberPlaylistsLib.Types;

namespace MapMaven.Models
{
    public class PlaylistMap
    {
        public string Hash { get; set; }

        public PlaylistMap(IPlaylistSong playlistSong)
        {
            Hash = playlistSong.Hash;
        }
    }
}
