using Microsoft.AspNetCore.Components;
using BeatSaberTools.Services;
using Map = BeatSaberTools.Models.Map;
using BeatSaberTools.Models;
using BeatSaberTools.Core.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace BeatSaberTools.Components.Maps
{
    public partial class MapBrowser : IDisposable
    {
        [Inject]
        protected MapService MapService { get; set; }

        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Parameter]
        public IEnumerable<Map> Maps { get; set; } = new List<Map>();
        private bool LoadingMapInfo = false;

        [Parameter]
        public RenderFragment? HeaderContent { get; set; }

        [Parameter]
        public RenderFragment<MapRowContext>? RowContent { get; set; }

        [Parameter]
        public bool Selectable { get; set; } = false;

        MudTable<Map> TableRef;

        private Playlist SelectedPlaylist = null;
        private IEnumerable<MapFilter> MapFilters = Enumerable.Empty<MapFilter>();

        private IEnumerable<string> MapHashFilter = null;

        private HashSet<Map> SelectedMaps = new HashSet<Map>();

        private string SearchString = "";

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += LocationChanged;

            SubscribeAndBind(BeatSaberDataService.LoadingMapInfo, loading => LoadingMapInfo = loading);
            SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist =>
            {
                SelectedPlaylist = selectedPlaylist;
                MapHashFilter = selectedPlaylist?.Maps.Select(m => m.Hash);
            });
            SubscribeAndBind(MapService.MapFilters, mapFilters => MapFilters = mapFilters);
            SubscribeAndBind(MapService.SelectedMaps, selectedMaps => SelectedMaps = selectedMaps);
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

            var mapFilter = MapFilters.All(f => f.Filter(map));

            return searchFilter && mapHashFilter && mapFilter;
        }

        protected void RemoveMapFilter(MapFilter mapFilter)
        {
            MapService.RemoveMapFilter(mapFilter);
        }

        void OnSelectedItemsChanged(HashSet<Map> selectedMaps)
        {
            MapService.SetSelectedMaps(selectedMaps);
        }

        private void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            MapService.ClearMapFilters();
            MapService.ClearSelectedMaps();
        }

        public IEnumerable<Map> GetFilteredMaps() => TableRef.FilteredItems;

        public void Dispose()
        {
            NavigationManager.LocationChanged -= LocationChanged;
        }
    }
}