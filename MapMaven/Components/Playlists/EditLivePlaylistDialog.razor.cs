using BeatSaberPlaylistsLib;
using FastDeepCloner;
using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components.Playlists
{
    public partial class EditLivePlaylistDialog
    {
        [Inject]
        protected IPlaylistService PlaylistService { get; set; }
        [Inject]
        protected LivePlaylistArrangementService LivePlaylistArrangementService { get; set; }
        [Inject]
        protected ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public EditLivePlaylistModel SelectedPlaylist { get; set; }

        [Parameter]
        public PlaylistManager PlaylistManager { get; set; }

        [Parameter]
        public bool NewPlaylist { get; set; } = true;

        bool LeaderboardAvailable = false;

        protected override void OnInitialized()
        {
            SubscribeAndBind(LeaderboardService.AvailableLeaderboardProviderServices, leaderboards => LeaderboardAvailable = leaderboards.Any());
        }

        void ConfigureLivePlaylist(EditLivePlaylistModel LivePlaylist)
        {
            SelectedPlaylist = DeepCloner.Clone(LivePlaylist);
            SelectedPlaylist.PlaylistManager = PlaylistManager;
        }

        void ConfigureCustomLivePlaylist()
        {
            SelectedPlaylist = new()
            {
                PlaylistManager = PlaylistManager,
                LivePlaylistConfiguration = new()
                {
                    MapCount = 20
                },
            };
        }

        void AddFilterOperation()
        {
            SelectedPlaylist.LivePlaylistConfiguration.FilterOperations.Add(new());
        }

        void AddSortOperation()
        {
            SelectedPlaylist.LivePlaylistConfiguration.SortOperations.Add(new());
        }

        void RemoveFilterOperation(FilterOperation filterOperation)
        {
            SelectedPlaylist.LivePlaylistConfiguration.FilterOperations.Remove(filterOperation);
        }

        void RemoveSortOperation(SortOperation sortOperation)
        {
            SelectedPlaylist.LivePlaylistConfiguration.SortOperations.Remove(sortOperation);
        }

        async Task ChangeMapPool(MapPool mapPool)
        {
            if (mapPool == SelectedPlaylist.LivePlaylistConfiguration.MapPool)
                return;

            var configuration = SelectedPlaylist.LivePlaylistConfiguration;

            if (configuration.FilterOperations.Any() || configuration.SortOperations.Any())
            {
                var result = await DialogService.ShowMessageBox(
                    title: string.Empty,
                    message: "Changing the map pool will remove all filter operations and sort operations. Are you sure you want to change the map pool?",
                    yesText: "Yes",
                    cancelText: "No"
                );

                if (result == null)
                {
                    return;
                }
                else
                {
                    configuration.FilterOperations.Clear();
                    configuration.SortOperations.Clear();
                }
            }

            configuration.MapPool = mapPool;
        }

        async Task OnValidSubmit()
        {
            Playlist playlist;

            if (NewPlaylist)
            {
                playlist = await PlaylistService.AddLivePlaylist(SelectedPlaylist);
                Snackbar.Add($"Added playlist \"{SelectedPlaylist.Name}\"", Severity.Normal, config =>
                {
                    config.Icon = Icons.Material.Filled.Check;

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
                playlist = await PlaylistService.EditLivePlaylist(SelectedPlaylist);
                Snackbar.Add($"Saved playlist \"{SelectedPlaylist.Name}\"", Severity.Normal, config =>
                {
                    config.Icon = Icons.Material.Filled.Check;

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
            
            Task.Run(LivePlaylistArrangementService.ArrangeLivePlaylists);

            MudDialog.Close(DialogResult.Ok(playlist));
        }

        void Cancel() => MudDialog.Cancel();
        void CancelConfiguration()
        {
            if (NewPlaylist)
            {
                SelectedPlaylist = null;
            }
            else
            {
                Cancel();
            }
        }
    }
}