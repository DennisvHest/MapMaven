using BeatSaberPlaylistsLib.Types;
using BeatSaverSharp.Models;
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
        public IEnumerable<PlaylistMap> Maps { get; set; }

        public DynamicPlaylistConfiguration DynamicPlaylistConfiguration { get; set; }

        public bool IsDynamicPlaylist => DynamicPlaylistConfiguration != null;

        public Lazy<string?> CoverImage { get; private set; }

        private IPlaylist _playlist;

        public Playlist(IPlaylist playlist)
        {
            _playlist = playlist;
            FileName = playlist.Filename;
            Title = playlist.Title;
            Description = playlist.Description;

            CoverImage = new Lazy<string?>(() =>
            {
                try
                {
                    Image coverImage = null;

                    using (var coverImageStream = _playlist.GetCoverStream())
                    {
                        if (coverImageStream != null && _playlist.HasCover)
                            coverImage = Image.FromStream(coverImageStream);

                        using (coverImage)
                        {
                            return coverImage?.ToDataUrl();
                        }
                    }
                }
                catch
                {
                    /* Ignore invalid cover images */
                    return null;
                }
            });

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
