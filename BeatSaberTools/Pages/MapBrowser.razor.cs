using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using BeatSaberTools.Models.Data;
using BeatSaberTools.Services;

namespace BeatSaberTools.Pages
{
    public partial class MapBrowser
    {
        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        private IEnumerable<MapInfo> Maps = new List<MapInfo>();
        protected override async Task OnInitializedAsync()
        {
            Maps = await BeatSaberDataService.GetAllMapInfo();
        }
    }
}