using BeatSaberPlaylistsLib;

namespace MapMaven.Core.Models.Data.Playlists
{
    public abstract class PlaylistTreeItem<T>
    {
        public PlaylistManager PlaylistManager { get; set; }

        protected PlaylistTreeItem(PlaylistManager playlistManager)
        {
            PlaylistManager = playlistManager;
        }

        public abstract IEnumerable<T> GetPlaylists();
    }
}
