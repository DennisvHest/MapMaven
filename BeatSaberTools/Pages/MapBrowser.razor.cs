using Microsoft.AspNetCore.Components;
using BeatSaberTools.Services;
using Map = BeatSaberTools.Models.Map;
using BeatSaberTools.Models;
using BeatSaberTools.Extensions;

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

        private Playlist SelectedPlaylist = null;
        private string SelectedSongAuthorName = null;

        private IEnumerable<string> MapHashFilter = null;

        private string SearchString = "";

        protected override void OnInitialized()
        {
            SubscribeAndBind(MapService.Maps, maps => Maps = maps);
            SubscribeAndBind(BeatSaberDataService.LoadingMapInfo, loading => LoadingMapInfo = loading);
            SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist =>
            {
                SelectedPlaylist = selectedPlaylist;
                MapHashFilter = selectedPlaylist?.Maps.Select(m => m.Hash);
            });
            SubscribeAndBind(MapService.SelectedSongAuthorName, selectedSongAuthor => SelectedSongAuthorName = selectedSongAuthor);
        }

        private bool Filter(Map map)
        {
            var searchFilter = true;

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var searchString = SearchString.Trim();

                searchFilter = $"{map.Name} {map.SongAuthorName} {map.MapAuthorName}".Contains(searchString, StringComparison.OrdinalIgnoreCase);
            }

            var mapHashFilter = MapHashFilter?.ToList() switch
            {
                null => true,
                [] => false,
                _ => MapHashFilter.Contains(map.Hash)
            };

            var songAuthorFilter = true;

            if (!string.IsNullOrEmpty(SelectedSongAuthorName))
            {
                songAuthorFilter = map.SongAuthorName == SelectedSongAuthorName;
            }

            return searchFilter && mapHashFilter && songAuthorFilter;
        }

        protected void RemoveSongAuthorFilter()
        {
            MapService.SelectSongAuthor(null);
        }
    }
}