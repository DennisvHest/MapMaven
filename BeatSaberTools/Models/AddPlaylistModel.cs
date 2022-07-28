using System.ComponentModel.DataAnnotations;

namespace BeatSaberTools.Models
{
    public class AddPlaylistModel
    {
        [Required]
        public string Name { get; set; }
    }
}
