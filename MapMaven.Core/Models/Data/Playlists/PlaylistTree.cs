using BeatSaberPlaylistsLib;

namespace MapMaven.Core.Models.Data.Playlists
{
    public class PlaylistTree<T>
    {
        public PlaylistFolder<T> RootPlaylistFolder { get; private set; }

        public PlaylistTree(PlaylistManager playlistManager)
        {
            RootPlaylistFolder = new PlaylistFolder<T>(playlistManager);
        }

        public IEnumerable<T> AllPlaylists()
        {
            return RootPlaylistFolder.GetPlaylists();
        }
    }
}
