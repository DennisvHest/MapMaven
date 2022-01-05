using Microsoft.AspNetCore.Components;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using BeatSaberTools.Extensions;
using System.Threading.Tasks;

namespace BeatSaberTools.Pages
{
    public partial class MapBrowserRow
    {
        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        [Parameter]
        public Map Map { get; set; }

        protected string CoverImageDataUrl { get; set; }

        protected override void OnInitialized()
        {
            InvokeAsync(() =>
            {
                var coverImage = BeatSaberDataService.GetMapCoverImage(Map.Id);

                CoverImageDataUrl = coverImage
                    .GetResizedImage(50, 50)
                    .ToDataUrl();

                StateHasChanged();
            });
        }
    }
}