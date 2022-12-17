using BeatSaberTools.Extensions;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Shared
{
    public partial class RankedMapBrowserRow
    {
        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected MapService MapService { get; set; }

        [Parameter]
        public Map Map { get; set; }

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
    }
}