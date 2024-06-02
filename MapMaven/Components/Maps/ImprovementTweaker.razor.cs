using MapMaven.Components.Playlists;
using MapMaven.Components.Shared;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Extensions;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = MapMaven.Models.Map;

namespace MapMaven.Components.Maps
{
    public partial class ImprovementTweaker
    {
        [Inject]
        IMapService MapService { get; set; }

        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public EventCallback<MapSelectionConfig> OnMapSelectionChanged { get; set; }

        string PlayedFilter = "Both";
        MapFilter PlayedMapFilter = null;

        string HiddenFilter = "Not hidden";
        MapFilter HiddenMapFilter = null;

        double MinimumPredictedAccuracy = 80;
        double MaximumPredictedAccuracy = 100;
        MapFilter MinimumPredictedAccuracyFilter = null;
        MapFilter MaximumPredictedAccuracyFilter = null;

        MapFilter TagsFilter = null;

        HashSet<Map> SelectedMaps = new();

        bool CreatingPlaylist = false;

        int MapSelectNumber = 10;
        int MapSelectStartFromNumber = 1;

        HashSet<string> MapTags = new();

        protected override void OnInitialized()
        {
            SubscribeAndBind(MapService.SelectedMaps, selectedMaps => SelectedMaps = selectedMaps);
            SubscribeAndBind(PlaylistService.CreatingPlaylist, creatingPlaylist => CreatingPlaylist = creatingPlaylist);

            LeaderboardService.PlayerScores
                .Where(playerScores => playerScores is not null)
                .Take(1)
                .Subscribe(playerScores =>
                {
                    var rankedScoresCount = playerScores.Count(x => x.Leaderboard.Ranked);

                    // Not enough ranked scores to make a good prediction, so lower the minimum predicted accuracy
                    if (rankedScoresCount < 10)
                    {
                        MinimumPredictedAccuracy = 65;
                    }
                    else if (rankedScoresCount < 20)
                    {
                        MinimumPredictedAccuracy = 75;
                    }

                    StateHasChanged();
                });

            SubscribeAndBind(MapService.RankedMaps, rankedMaps =>
                MapTags = rankedMaps
                    .SelectMany(map => map.Tags)
                    .OrderBy(tag => tag)
                    .ToHashSet()
            );
        }

        protected override void OnAfterRender(bool firstRender)
        {
            OnHiddenFilterChanged(HiddenFilter);
            OnMinimumPredictedAccuracyFilterChanged(MinimumPredictedAccuracy);
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
                Name = PlayedFilter,
                Visible = false
            };

            PlayedMapFilter.Filter = PlayedFilter switch
            {
                "Not played" => map => map.HighestPlayerScore == null,
                "Played" => map => map.HighestPlayerScore != null
            };

            MapService.AddMapFilter(PlayedMapFilter);
        }

        void OnHiddenFilterChanged(string value)
        {
            HiddenFilter = value;

            if (HiddenMapFilter != null)
                MapService.RemoveMapFilter(HiddenMapFilter);

            if (HiddenFilter == "Both")
            {
                HiddenMapFilter = null;
                return;
            }

            HiddenMapFilter = new MapFilter
            {
                Name = HiddenFilter,
                Visible = false
            };

            HiddenMapFilter.Filter = HiddenFilter switch
            {
                "Not hidden" => map => !map.Hidden,
                "Hidden" => map => map.Hidden
            };

            MapService.AddMapFilter(HiddenMapFilter);
        }

        void OnMinimumPredictedAccuracyFilterChanged(double value)
        {
            MinimumPredictedAccuracy = value;

            if (MinimumPredictedAccuracyFilter != null)
                MapService.RemoveMapFilter(MinimumPredictedAccuracyFilter);

            MinimumPredictedAccuracyFilter = new MapFilter
            {
                Name = $"Minimum predicted accuracy >= {MinimumPredictedAccuracy}%",
                Visible = false,
                Filter = map => map.ScoreEstimate?.Accuracy >= MinimumPredictedAccuracy
            };

            MapService.AddMapFilter(MinimumPredictedAccuracyFilter);
        }

        void OnMaximumPredictedAccuracyFilterChanged(double value)
        {
            MaximumPredictedAccuracy = value;

            if (MaximumPredictedAccuracyFilter != null)
                MapService.RemoveMapFilter(MaximumPredictedAccuracyFilter);

            MaximumPredictedAccuracyFilter = new MapFilter
            {
                Name = $"Maximum predicted accuracy <= {MaximumPredictedAccuracy}%",
                Visible = false,
                Filter = map => map.ScoreEstimate?.Accuracy <= MaximumPredictedAccuracy
            };

            MapService.AddMapFilter(MaximumPredictedAccuracyFilter);
        }

        void OnTagsFilterChanged(IEnumerable<string> tags)
        {
            if (TagsFilter != null)
                MapService.RemoveMapFilter(TagsFilter);

            TagsFilter = new MapFilter
            {
                Name = $"Has tags: {string.Join(", ", tags)}",
                Visible = false,
                Filter = map =>
                {
                    if (!tags.Any() || map.Tags is null)
                        return true;

                    return tags.All(t => map.Tags.Contains(t));
                }
            };

            MapService.AddMapFilter(TagsFilter);
        }

        async Task CreatePlaylistFromSelectedMaps()
        {
            var editPlaylistDialog = await DialogService.ShowAsync<EditPlaylistDialog>(
                title: "Add improvement playlist",
                parameters: new()
                {
                    { nameof(EditPlaylistDialog.SavePlaylistOnSubmit), false },
                    {
                        nameof(EditPlaylistDialog.EditPlaylistModel),
                        new EditPlaylistModel { Name = $"Improvement Maps ({DateTime.Now:dd-MM-yyyy HH:mm:ss})" }
                    }
                },
                options: new()
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );

            var result = await editPlaylistDialog.Result;

            if (result.Canceled)
                return;

            var playlistModel = (EditPlaylistModel)result.Data;

            var snackbar = Snackbar.AddMapDownloadProgressSnackbar();

            var playlist = await PlaylistService.AddPlaylistAndDownloadMaps(playlistModel, SelectedMaps, progress: snackbar.Progress, cancellationToken: snackbar.CancellationToken);

            MapService.ClearSelectedMaps();

            Snackbar.Remove(snackbar.Snackbar);

            if (!snackbar.CancellationToken.IsCancellationRequested)
            {
                Snackbar.Add($"Created playlist: {playlistModel.Name}", Severity.Normal, config =>
                {
                    config.Icon = Icons.Filled.Check;

                    config.Action = "Open";
                    config.ActionColor = MudBlazor.Color.Primary;
                    config.Onclick = snackbar =>
                    {
                        PlaylistService.SetSelectedPlaylist(playlist);
                        NavigationManager.NavigateTo("/maps");
                        return Task.CompletedTask;
                    };
                });
            }
            else
            {
                Snackbar.Add($"Cancelled creating playlist.", Severity.Normal, config => config.Icon = Icons.Material.Filled.Cancel);
            }
        }

        void ClearSelection()
        {
            MapService.ClearSelectedMaps();
        }

        async Task ApplyMapSelection()
        {
            await OnMapSelectionChanged.InvokeAsync(new MapSelectionConfig
            {
                MapSelectNumber = MapSelectNumber,
                MapSelectStartFromNumber = MapSelectStartFromNumber
            });
        }

        async Task HideUnhideSelectedMaps(bool hide)
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to {(hide ? "hide" : "un-hide")} the selected ({SelectedMaps.Count}) maps?" },
                { nameof(ConfirmationDialog.ConfirmText), hide ? "Hide" : "Un-hide" }
            });

            var result = await dialog.Result;

            if (result.Cancelled)
                return;

            await MapService.HideUnhideMap(SelectedMaps, hide);

            ClearSelection();
        }
    }
}