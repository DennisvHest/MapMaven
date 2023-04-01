using MapMaven.Extensions;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using Map = MapMaven.Models.Map;

namespace MapMaven.Components.Maps
{
    public partial class RankedMapBrowserRow
    {
        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected MapService MapService { get; set; }

        [Parameter]
        public Map Map { get; set; }

        [Parameter]
        public IEnumerable<Map> FilteredMaps { get; set; }

        string GetCoverImageUrl(Map map)
        {
            if (!string.IsNullOrEmpty(map.CoverImageUrl))
                return map.CoverImageUrl;

            var coverImage = BeatSaberDataService.GetMapCoverImage(map.Hash);

            return coverImage
                .GetResizedImage(50, 50)
                .ToDataUrl();
        }

        bool MapIsInstalled(Map map)
        {
            return MapService.MapIsInstalled(map);
        }

        async Task DownloadMap(Map map)
        {
            await MapService.DownloadMap(map);
        }

        async Task HideUnhideMap(Map map)
        {
            await MapService.HideUnhideMap(map);
        }
    }
}