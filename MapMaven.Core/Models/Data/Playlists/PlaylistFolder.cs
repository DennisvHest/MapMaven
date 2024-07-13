using BeatSaberPlaylistsLib;

namespace MapMaven.Core.Models.Data.Playlists
{
    public class PlaylistFolder<T> : PlaylistTreeItem<T>
    {
        public List<PlaylistTreeItem<T>> ChildItems { get; set; } = [];

        public PlaylistFolder(PlaylistManager playlistManager) : base(playlistManager) { }

        public override IEnumerable<T> GetPlaylists()
        {
            foreach (var item in ChildItems)
            {
                foreach (var playlist in item.GetPlaylists())
                {
                    yield return playlist;
                }
            }
        }
    }
}
