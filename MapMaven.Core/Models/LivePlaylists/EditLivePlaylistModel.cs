using FastDeepCloner;
using MapMaven.Models;
using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.LivePlaylists
{
    public class EditLivePlaylistModel : EditPlaylistModel
    {
        [ValidateComplexType]
        public LivePlaylistConfiguration LivePlaylistConfiguration { get; set; } = new()
        {
            MapCount = 20
        };

        public EditLivePlaylistModel() { }

        public EditLivePlaylistModel(Playlist playlist) : base(playlist)
        {
            LivePlaylistConfiguration = DeepCloner.Clone(playlist.LivePlaylistConfiguration);
        }
    }
}
