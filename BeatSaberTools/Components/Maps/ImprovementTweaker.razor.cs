using BeatSaberTools.Core.Models;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Components.Maps
{
    public partial class ImprovementTweaker
    {
        [Inject]
        protected MapService MapService { get; set; }

        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        string PlayedFilter = "Both";
        MapFilter PlayedMapFilter = null;

        HashSet<Map> SelectedMaps = new();

        protected override void OnInitialized()
        {
            SubscribeAndBind(MapService.SelectedMaps, selectedMaps => SelectedMaps = selectedMaps);
        }

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

        async Task CreatePlaylistFromSelectedMaps()
        {
            var playlistModel = new EditPlaylistModel
            {
                Name = $"Improvement Maps ({DateTime.Now:dd-MM-yyyy HH:mm:ss})",
                FileName = $"Improvement Maps ({DateTime.Now:dd-MM-yyyy_HH-mm-ss})"
            };

            await PlaylistService.AddPlaylistAndDownloadMaps(playlistModel, SelectedMaps);
        }
    }
}