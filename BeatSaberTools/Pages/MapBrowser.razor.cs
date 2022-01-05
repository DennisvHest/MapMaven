using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using BeatSaberTools.Services;
using BeatSaberTools.Models;
using System;
using BeatSaberTools.Extensions;

namespace BeatSaberTools.Pages
{
    public partial class MapBrowser
    {
        [Inject]
        protected MapService MapService { get; set; }

        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        private IEnumerable<Map> Maps = new List<Map>();
        private bool LoadingMapInfo = false;

        private string SearchString = "";

        protected override void OnInitialized()
        {
            MapService.Maps.Subscribe(maps =>
            {
                Maps = maps;
                StateHasChanged();
            });

            BeatSaberDataService.LoadingMapInfo.Subscribe(loading =>
            {
                LoadingMapInfo = loading;
                StateHasChanged();
            });
        }

        private bool Filter(Map map)
        {
            if (string.IsNullOrWhiteSpace(SearchString))
                return true;

            var searchString = SearchString.Trim();

            return $"{map.Name} {map.SongAuthorName} {map.MapAuthorName}".Contains(searchString, StringComparison.OrdinalIgnoreCase);
        }
    }
}