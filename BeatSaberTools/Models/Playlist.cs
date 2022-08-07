using BeatSaberPlaylistsLib.Types;
using BeatSaberTools.Extensions;
using Image = System.Drawing.Image;

namespace BeatSaberTools.Models
{
    public class Playlist
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }
        public IEnumerable<PlaylistMap> Maps { get; set; }

        public Playlist(IPlaylist playlist)
        {
            FileName = playlist.Filename;
            Title = playlist.Title;
            Description = playlist.Description;

            Image coverImage = null;

            var coverImageStream = playlist.GetCoverStream();

            if (coverImageStream != null && playlist.HasCover)
                coverImage = Image.FromStream(coverImageStream);

            CoverImage = coverImage?.ToDataUrl();

            Maps = playlist.Select(s => new PlaylistMap(s));
        }
    }
}
