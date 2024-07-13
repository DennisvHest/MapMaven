using BeatSaberPlaylistsLib.Types;

namespace MapMaven.Core.Models.Data.Playlists
{
    public class PlaylistTree<T>
    {
        public List<PlaylistTreeItem<T>> Items { get; set; } = [];

        public IEnumerable<T> AllPlaylists()
        {
            foreach (var item in Items)
            {
                foreach (var playlist in item.GetPlaylists())
                {
                    yield return playlist;
                }
            }
        }
    }
}
