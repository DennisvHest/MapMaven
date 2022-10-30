using BeatSaberTools.Models;

namespace BeatSaberTools.Core.Models.DynamicPlaylists
{
    public class EditDynamicPlaylistModel : EditPlaylistModel
    {
        public DynamicPlaylistConfiguration DynamicPlaylistConfiguration { get; set; }
    }
}
