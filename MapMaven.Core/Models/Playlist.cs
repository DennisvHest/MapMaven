using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Extensions;
using Newtonsoft.Json.Linq;
using Image = System.Drawing.Image;

namespace MapMaven.Models
{
    public class Playlist
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }
        public IEnumerable<PlaylistMap> Maps { get; set; }

        public DynamicPlaylistConfiguration DynamicPlaylistConfiguration { get; set; }

        public bool IsDynamicPlaylist => DynamicPlaylistConfiguration != null;

        public Playlist(IPlaylist playlist)
        {
            FileName = playlist.Filename;
            Title = playlist.Title;
            Description = playlist.Description;

            try
            {
                Image coverImage = null;

                using (var coverImageStream = playlist.GetCoverStream())
                {
                    if (coverImageStream != null && playlist.HasCover)
                        coverImage = Image.FromStream(coverImageStream);

                    using (coverImage)
                    {
                        CoverImage = coverImage?.ToDataUrl();
                    }
                }
            }
            catch { /* Ignore invalid cover images */ }

            Maps = playlist.Select(s => new PlaylistMap(s));

            if (playlist.TryGetCustomData("mapMaven", out dynamic customData))
            {
                if (customData.dynamicPlaylistConfiguration is DynamicPlaylistConfiguration dynamicPlaylistConfiguration)
                {
                    DynamicPlaylistConfiguration = dynamicPlaylistConfiguration;
                }
                else if (customData.dynamicPlaylistConfiguration is JObject configuration)
                {
                    DynamicPlaylistConfiguration = configuration.ToObject<DynamicPlaylistConfiguration>();
                }
            }
        }
    }
}
