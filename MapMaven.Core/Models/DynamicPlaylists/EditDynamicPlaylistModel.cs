using FastDeepCloner;
using MapMaven.Models;
using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class EditDynamicPlaylistModel : EditPlaylistModel
    {
        [ValidateComplexType]
        public DynamicPlaylistConfiguration DynamicPlaylistConfiguration { get; set; } = new()
        {
            MapCount = 20
        };

        public EditDynamicPlaylistModel() { }

        public EditDynamicPlaylistModel(Playlist playlist) : base(playlist)
        {
            DynamicPlaylistConfiguration = DeepCloner.Clone(playlist.DynamicPlaylistConfiguration);
        }
    }
}
