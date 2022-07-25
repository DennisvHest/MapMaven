using BeatSaberPlaylistsLib.Types;

namespace BeatSaberTools.Models
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
