using BeatSaberPlaylistsLib;
using System.IO;

namespace MapMaven.Core.Models.Data.Playlists
{
    public class PlaylistFolder<T> : PlaylistTreeItem<T>
    {
        public string FolderName { get; set; } = string.Empty;
        public List<PlaylistTreeItem<T>> ChildItems { get; set; } = [];

        public PlaylistFolder() : base(null) { }

        public PlaylistFolder(PlaylistManager playlistManager) : base(playlistManager)
        {
            if (playlistManager is not null)
                FolderName = new DirectoryInfo(playlistManager.PlaylistPath).Name;
        }

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
