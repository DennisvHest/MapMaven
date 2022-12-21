using BeatSaberTools.Core.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;

namespace BeatSaberTools.Components.Maps
{
    public partial class ImprovementTweaker
    {
        [Inject]
        protected MapService MapService { get; set; }

        string PlayedFilter = "Both";
        MapFilter PlayedMapFilter = null;

        void OnPlayedFilterChanged(string value)
        {
            PlayedFilter = value;

            if (PlayedMapFilter != null)
                MapService.RemoveMapFilter(PlayedMapFilter);

            if (PlayedFilter == "Both")
            {
                PlayedMapFilter = null;
                return;
            }

            PlayedMapFilter = new MapFilter
            {
                Type = MapFilterType.Played,
                Name = PlayedFilter,
                Visible = false
            };

            PlayedMapFilter.Filter = PlayedFilter switch
            {
                "Not played" => map => map.PlayerScore == null,
                "Played" => map => map.PlayerScore != null
            };

            MapService.AddMapFilter(PlayedMapFilter);
        }
    }
}