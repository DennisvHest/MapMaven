using BeatSaberTools.Core.Models.DynamicPlaylists;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SortDirection = BeatSaberTools.Core.Models.DynamicPlaylists.SortDirection;

namespace BeatSaberTools.Shared
{
    public partial class EditDynamicPlaylistDialog
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        protected EditDynamicPlaylistModel SelectedPlaylist { get; set; }

        private static IEnumerable<EditDynamicPlaylistModel> DynamicPlaylists = new EditDynamicPlaylistModel[]
        {
            new EditDynamicPlaylistModel
            {
                FileName = "RECENTLY_ADDED_MAPS",
                Name = "Recently added maps",
                Description = "The most recently downloaded maps.",
                DynamicPlaylistConfiguration = new DynamicPlaylistConfiguration
                {
                    SortOperations = new[]
                    {
                        new SortOperation
                        {
                            Field = "AddedDateTime",
                            Direction = SortDirection.Descending
                        }
                    },
                    MapCount = 20
                }
            }
        };

        void ConfigureDynamicPlaylist(EditDynamicPlaylistModel dynamicPlaylist)
        {
            SelectedPlaylist = dynamicPlaylist;
        }

        async Task AddDynamicPlaylist()
        {
            await PlaylistService.AddDynamicPlaylist(SelectedPlaylist);

            Snackbar.Add($"Added playlist \"{SelectedPlaylist.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);

            MudDialog.Close(DialogResult.Ok(SelectedPlaylist));
        }

        void Cancel() => MudDialog.Cancel();
        void CancelConfiguration() => SelectedPlaylist = null;
    }
}