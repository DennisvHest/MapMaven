using System.ComponentModel.DataAnnotations;

namespace BeatSaberTools.Models
{
    public class AddPlaylistModel
    {
        [Required]
        public string Name { get; set; }
        public string CoverImage { get; set; }
    }
}
