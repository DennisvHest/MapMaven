using Microsoft.AspNetCore.Components;
using Map = MapMaven.Models.Map;
using MapMaven.Models;
using MapMaven.Core.Models;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services;
using System.Reactive.Linq;
using MapMaven.Components.Shared;
using MapMaven.Components.Playlists;
using MapMaven.Core.Services.Leaderboards.ScoreEstimation;

namespace MapMaven.Components.Maps
{
    public partial class MapBrowser : IDisposable
    {
        [Inject]
        protected IMapService MapService { get; set; }

        [Inject]
        protected IPlaylistService PlaylistService { get; set; }

        [Inject]
        protected IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        protected DynamicPlaylistArrangementService DynamicPlaylistArrangementService { get; set; }

        [Inject]
        protected IEnumerable<IScoreEstimationService> ScoreEstimationServices { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        ScoreEstimationSettings ScoreEstimationSettings { get; set; }

        [Parameter]
        public IEnumerable<Map> Maps { get; set; } = new List<Map>();
        private bool LoadingMapInfo = false;
        private bool InitialMapLoad = false;

        [Parameter]
        public RenderFragment? HeaderContent { get; set; }

        [Parameter]
        public RenderFragment<MapRowContext>? RowContent { get; set; }

        [Parameter]
        public string Width { get; set; } = "100%";

        [Parameter]
        public string Height { get; set; } = "calc(100% - 116px)";

        [Parameter]
        public bool RankedMaps { get; set; } = false;

        string Style => $"width: {Width}";

        MudDataGrid<Map> TableRef;

        private Playlist SelectedPlaylist = null;
        private IEnumerable<MapFilter> MapFilters = Enumerable.Empty<MapFilter>();

        private List<string> MapHashFilter = null;

        private HashSet<Map> SelectedMaps = new HashSet<Map>();
        private bool Selectable = false;

        private string SearchString = "";

        private int DifficultyModifier = 0;

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += LocationChanged;

            var estimatingScoresObservable = Observable.CombineLatest(ScoreEstimationServices.Select(s => s.EstimatingScores), x => x.Any(estimatingScores => estimatingScores));

            var loadingObservable = Observable.CombineLatest(
                BeatSaberDataService.LoadingMapInfo,
                DynamicPlaylistArrangementService.ArrangingDynamicPlaylists,
                estimatingScoresObservable,
                PlaylistService.SelectedPlaylist,
                (loadingMapInfo, arrangingDynamicPlaylists, estimatingScores, selectedPlaylist) =>
                    loadingMapInfo
                    || arrangingDynamicPlaylists && selectedPlaylist?.IsDynamicPlaylist == true
                    || estimatingScores
            );

            SubscribeAndBind(loadingObservable, loading => LoadingMapInfo = loading);
            SubscribeAndBind(BeatSaberDataService.InitialMapLoad, initialMapLoad => InitialMapLoad = initialMapLoad);
            SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist =>
            {
                SelectedPlaylist = selectedPlaylist;
                MapHashFilter = selectedPlaylist?.Maps.Select(m => m.Hash).ToList();
                SortMapsWithDefaultSort();
            });
            SubscribeAndBind(MapService.MapFilters, mapFilters => MapFilters = mapFilters);
            SubscribeAndBind(MapService.SelectedMaps, selectedMaps => SelectedMaps = selectedMaps);
            SubscribeAndBind(MapService.Selectable, selectable => Selectable = selectable);

            SubscribeAndBind(ScoreEstimationSettings.DifficultyModifierValue, difficultyModifierValue => DifficultyModifier = difficultyModifierValue);
        }

        protected override void OnParametersSet()
        {
            SortMapsWithDefaultSort();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (RankedMaps)
                await TableRef.SetSortAsync("ScoreEstimate", SortDirection.Descending, x => x.ScoreEstimates.Any() ? x.ScoreEstimates.Max(x => x.PPIncrease) : 0);
        }

        private void SortMapsWithDefaultSort()
        {
            if (RankedMaps)
                return;

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
            MapService.CancelSelection();
        }

        public IEnumerable<Map> GetFilteredMaps() => TableRef.FilteredItems;

        void CancelSelection() => MapService.CancelSelection();

        async Task DeleteSelectedMaps()
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to delete the selected ({SelectedMaps.Count}) maps from the game? This cannot be undone." },
                { nameof(ConfirmationDialog.ConfirmText), $"Delete" }
            });

            var result = await dialog.Result;

            if (result.Cancelled)
                return;

            await MapService.DeleteMaps(SelectedMaps.Select(m => m.Hash));

            MapService.CancelSelection();

            Snackbar.Add($"Succesfully deleted selected maps", Severity.Normal, config => config.Icon = Icons.Filled.Check);
        }

        async Task AddSelectedMapsToPlaylist()
        {
            var dialog = await DialogService.ShowAsync<PlaylistSelector>($"Add selected maps ({SelectedMaps.Count}) to playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                CloseButton = true
            });

            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                var playlist = (Playlist)result.Data;

                await PlaylistService.AddMapsToPlaylist(SelectedMaps, playlist);

                Snackbar.Add($"Added selected maps to \"{playlist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
        }

        async Task RemoveSelectedMapsFromSelectedPlaylist()
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to remove the selected ({SelectedMaps.Count}) maps from the \"{SelectedPlaylist.Title}\" playlist?" },
                { nameof(ConfirmationDialog.ConfirmText), $"Remove" }
            });

            var result = await dialog.Result;

            if (result.Cancelled)
                return;

            await PlaylistService.RemoveMapsFromPlaylist(SelectedMaps, SelectedPlaylist);

            MapService.CancelSelection();

            Snackbar.Add($"Removed selected maps from \"{SelectedPlaylist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
        }

        public void ToggleSelectable() => MapService.SetSelectable(!Selectable);

        public void Dispose()
        {
            NavigationManager.LocationChanged -= LocationChanged;
        }
    }
}