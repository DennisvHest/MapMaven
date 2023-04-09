using Microsoft.AspNetCore.Components;
using MapMaven.Services;
using Map = MapMaven.Models.Map;
using MapMaven.Models;
using MapMaven.Core.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace MapMaven.Components.Maps
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
        private bool InitialMapLoad = false;

        [Parameter]
        public RenderFragment? HeaderContent { get; set; }

        [Parameter]
        public RenderFragment<MapRowContext>? RowContent { get; set; }

        [Parameter]
        public bool Selectable { get; set; } = false;

        [Parameter]
        public string Width { get; set; } = "100%";

        [Parameter]
        public string Height { get; set; } = "calc(100% - 116px)";

        string Style => $"width: {Width}";

        MudDataGrid<Map> TableRef;

        private Playlist SelectedPlaylist = null;
        private IEnumerable<MapFilter> MapFilters = Enumerable.Empty<MapFilter>();

        private List<string> MapHashFilter = null;

        private HashSet<Map> SelectedMaps = new HashSet<Map>();

        private string SearchString = "";

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += LocationChanged;

            SubscribeAndBind(BeatSaberDataService.LoadingMapInfo, loading => LoadingMapInfo = loading);
            SubscribeAndBind(BeatSaberDataService.InitialMapLoad, initialMapLoad => InitialMapLoad = initialMapLoad);
            SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist =>
            {
                SelectedPlaylist = selectedPlaylist;
                MapHashFilter = selectedPlaylist?.Maps.Select(m => m.Hash).ToList();
                SortMapsWithDefaultSort();
            });
            SubscribeAndBind(MapService.MapFilters, mapFilters => MapFilters = mapFilters);
            SubscribeAndBind(MapService.SelectedMaps, selectedMaps => SelectedMaps = selectedMaps);
        }

        protected override void OnParametersSet()
        {
            SortMapsWithDefaultSort();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await TableRef.SetSortAsync("ScoreEstimate", SortDirection.Descending, x => x.ScoreEstimates.Any() ? x.ScoreEstimates.Max(x => x.PPIncrease) : 0);
        }

        private void SortMapsWithDefaultSort()
        {
            // If a playlist is selected, preserve the order of the playlist. Otherwise, sort by AddedDateTime.
            if (MapHashFilter != null)
            {
                Maps = Maps.OrderBy(m => MapHashFilter.IndexOf(m.Hash)).ToList();
            }
            else
            {
                Maps = Maps.OrderByDescending(m => m.AddedDateTime).ToList();
            }
        }

        private bool Filter(Map map)
        {
            var searchFilter = true;

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var searchString = SearchString.Trim();

                searchFilter = $"{map.Name} {map.SongAuthorName} {map.MapAuthorName}".Contains(searchString, StringComparison.OrdinalIgnoreCase);
            }

            var mapHashFilter = MapHashFilter switch
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