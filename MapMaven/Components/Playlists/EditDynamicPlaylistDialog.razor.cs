using FastDeepCloner;
using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components.Playlists
{
    public partial class EditDynamicPlaylistDialog
    {
        [Inject]
        protected IPlaylistService PlaylistService { get; set; }
        [Inject]
        protected DynamicPlaylistArrangementService DynamicPlaylistArrangementService { get; set; }
        [Inject]
        protected ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public EditDynamicPlaylistModel SelectedPlaylist { get; set; }

        [Parameter]
        public bool NewPlaylist { get; set; } = true;

        bool LeaderboardAvailable = false;

        protected override void OnInitialized()
        {
            SubscribeAndBind(LeaderboardService.AvailableLeaderboardProviderServices, leaderboards => LeaderboardAvailable = leaderboards.Any());
        }

        void ConfigureDynamicPlaylist(EditDynamicPlaylistModel dynamicPlaylist)
        {
            SelectedPlaylist = DeepCloner.Clone(dynamicPlaylist);
        }

        void ConfigureCustomDynamicPlaylist()
        {
            SelectedPlaylist = new()
            {
                DynamicPlaylistConfiguration = new()
                {
                    MapCount = 20
                },
            };
        }

        void AddFilterOperation()
        {
            SelectedPlaylist.DynamicPlaylistConfiguration.FilterOperations.Add(new());
        }

        void AddSortOperation()
        {
            SelectedPlaylist.DynamicPlaylistConfiguration.SortOperations.Add(new());
        }

        void RemoveFilterOperation(FilterOperation filterOperation)
        {
            SelectedPlaylist.DynamicPlaylistConfiguration.FilterOperations.Remove(filterOperation);
        }

        void RemoveSortOperation(SortOperation sortOperation)
        {
            SelectedPlaylist.DynamicPlaylistConfiguration.SortOperations.Remove(sortOperation);
        }

        async Task ChangeMapPool(MapPool mapPool)
        {
            if (mapPool == SelectedPlaylist.DynamicPlaylistConfiguration.MapPool)
                return;

            var configuration = SelectedPlaylist.DynamicPlaylistConfiguration;

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
                playlist = await PlaylistService.AddDynamicPlaylist(SelectedPlaylist);
                Snackbar.Add($"Added playlist \"{SelectedPlaylist.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
            else
            {
                playlist = await PlaylistService.EditDynamicPlaylist(SelectedPlaylist);
                Snackbar.Add($"Saved playlist \"{SelectedPlaylist.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
            
            Task.Run(DynamicPlaylistArrangementService.ArrangeDynamicPlaylists);

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