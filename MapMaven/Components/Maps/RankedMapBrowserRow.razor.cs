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
        public int RowNumber { get; set; }

        protected string CoverImageUrl { get; set; }

        bool MapInstalled { get; set; }

        protected override void OnInitialized()
        {
            if (string.IsNullOrEmpty(Map.CoverImageUrl))
            {
                Task.Run(() =>
                {
                    var coverImage = BeatSaberDataService.GetMapCoverImage(Map.Hash);

                    CoverImageUrl = coverImage
                        .GetResizedImage(50, 50)
                        .ToDataUrl();

                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                CoverImageUrl = Map.CoverImageUrl;
            }

            MapInstalled = MapService.MapIsInstalled(Map);
        }

        async Task DownloadMap()
        {
            await MapService.DownloadMap(Map);
            MapInstalled = true;
        }

        async Task HideUnhideMap()
        {
            await MapService.HideUnhideMap(Map);
        }
    }
}