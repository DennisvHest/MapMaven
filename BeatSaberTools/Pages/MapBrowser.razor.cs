using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using BeatSaberTools.Services;
using BeatSaberTools.Models;
using System;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Pages
{
    public partial class MapBrowser
    {
        [Inject]
        protected MapService MapService { get; set; }

        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        private IEnumerable<Map> Maps = new List<Map>();
        private bool LoadingMapInfo = false;

        private IEnumerable<string> MapHashFilter = Array.Empty<string>();

        private string SearchString = "";

        protected override void OnInitialized()
        {
            MapService.Maps.Subscribe(maps =>
            {
                Maps = maps;
                InvokeAsync(StateHasChanged);
            });

            BeatSaberDataService.LoadingMapInfo.Subscribe(loading =>
            {
                LoadingMapInfo = loading;
                InvokeAsync(StateHasChanged);
            });

            PlaylistService.SelectedPlaylist.Subscribe(selectedPlaylist =>
            {
                MapHashFilter = selectedPlaylist?.Maps.Select(m => m.Hash) ?? Array.Empty<string>();
                InvokeAsync(StateHasChanged);
            });
        }

        private bool Filter(Map map)
        {
            var searchFilter = true;

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var searchString = SearchString.Trim();

                searchFilter = $"{map.Name} {map.SongAuthorName} {map.MapAuthorName}".Contains(searchString, StringComparison.OrdinalIgnoreCase);
            }

            var mapHashFilter = true;

            if (MapHashFilter.Any())
            {
                mapHashFilter = MapHashFilter.Contains(map.Hash);
            }

            return searchFilter && mapHashFilter;
        }
    }
}