using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models
{
    public class EditPlaylistFolderModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
