
using BeatSaberPlaylistsLib;

namespace MapMaven.Core.Models.Data.Playlists
{
    public class PlaylistTreeNode<T> : PlaylistTreeItem<T>
    {
        public T Playlist { get; set; }

        public PlaylistTreeNode(T playlist, PlaylistManager playlistManager) : base(playlistManager)
        {
            Playlist = playlist;
        }

        public override IEnumerable<T> GetPlaylists()
        {
            yield return Playlist;
        }

        public override PlaylistTreeItem<T> Copy()
        {
            return new PlaylistTreeNode<T>(Playlist, PlaylistManager);
        }
    }
}
