using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;

namespace MapMaven.Core.Extensions
{
    public static class PlaylistManagerExtensions
    {
        public static void SavePlaylist(this PlaylistManager playlistManager, IPlaylist playlist)
        {
            var managerForPlaylist = playlistManager.GetManagerForPlaylist(playlist);

            managerForPlaylist ??= playlistManager;

            managerForPlaylist.StorePlaylist(playlist);
        }
    }
}
