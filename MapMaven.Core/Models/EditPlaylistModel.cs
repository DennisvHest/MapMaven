using BeatSaberPlaylistsLib;
using System.ComponentModel.DataAnnotations;

namespace MapMaven.Models
{
    public class EditPlaylistModel
    {
        public string FileName { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }

        public PlaylistManager OriginalPlaylistManager { get; set; }
        public PlaylistManager PlaylistManager { get; set; }

        public EditPlaylistModel() { }

        public EditPlaylistModel(Playlist playlist)
        {
            FileName = playlist.FileName;
            Name = playlist.Title;
            Description = playlist.Description;
            CoverImage = playlist.CoverImage.Value;
            PlaylistManager = playlist.PlaylistManager;
            OriginalPlaylistManager = playlist.PlaylistManager;
        }
    }
}
