using MapMaven.Models;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class EditDynamicPlaylistModel : EditPlaylistModel
    {
        public DynamicPlaylistConfiguration DynamicPlaylistConfiguration { get; set; }
    }
}
