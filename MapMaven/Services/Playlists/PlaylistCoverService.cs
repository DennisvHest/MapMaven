using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities;
using MapMaven.Models;
using Microsoft.VisualStudio.Threading;
using System.Reactive.Linq;
using MapMaven.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MapMaven.Services.Playlists
{
    public class PlaylistCoverService
    {
        private Dictionary<Playlist, AsyncLazy<string>> _cachedCoverImages = new(new CachedPlaylistEqualityComparer());

        public PlaylistCoverService(IMapService mapService, IPlaylistService playlistService, IBeatSaberDataService beatSaberDataService)
        {
            Observable
                .CombineLatest(playlistService.Playlists, mapService.MapsByHash, (playlists, maps) => (playlists, maps))
                .Subscribe((result) =>
                {
                    // Remove playlists that no longer exist
                    foreach (var playlist in _cachedCoverImages.Keys.Except(result.playlists))
                    {
                        _cachedCoverImages.Remove(playlist);
                    }

                    // Add new playlists
                    foreach (var playlist in result.playlists)
                    {
                        var coverImage = new AsyncLazy<string>(async () =>
                        {
                            var coverImage = playlist.CoverImageSmall.Value;

                            if (playlist.HasCover)
                                return coverImage;

                            var mapImages = playlist.Maps
                                .Select(playlistMap => result.maps.GetValueOrDefault(playlistMap.Hash))
                                .Where(map => map is not null)
                                .Take(4)
                                .Select(map => beatSaberDataService.GetMapCoverImageStream(map.Hash))
                                .ToList();

                            using var completeImage = mapImages.Count switch
                            {
                                1 => mapImages[0],
                                2 => await ImageUtilities.GenerateCollage(mapImages[0], mapImages[1]),
                                3 => await ImageUtilities.GenerateCollage(mapImages[0], mapImages[1], mapImages[2]),
                                4 => await ImageUtilities.GenerateCollage(mapImages[0], mapImages[1], mapImages[2], mapImages[3]),
                                _ => null
                            };

                            if (completeImage == null)
                                return null;

                            using var image = System.Drawing.Image.FromStream(completeImage);

                            using var resizedImage = image.GetResizedImage(50, 50);

                            return image.ToDataUrl();
                        });

                        _cachedCoverImages.Add(playlist, coverImage);
                    }
                });
        }

        public async Task<string> GetCoverImageAsync(Playlist playlist)
        {
            if (_cachedCoverImages.TryGetValue(playlist, out var coverImage))
                return await coverImage.GetValueAsync();

            return null;
        }
    }

    public class CachedPlaylistEqualityComparer : IEqualityComparer<Playlist>
    {
        public bool Equals(Playlist x, Playlist y) => ReferenceEquals(x, y);

        public int GetHashCode([DisallowNull] Playlist obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
