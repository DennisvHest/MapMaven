using BeatSaberTools.Models;

namespace BeatSaberTools.Core.Models
{
    public class EditDynamicPlaylistModel : EditPlaylistModel
    {
        public DynamicPlaylistConfiguration DynamicPlaylistConfiguration { get; set; }
        public int Test { get; set; }
    }
}
