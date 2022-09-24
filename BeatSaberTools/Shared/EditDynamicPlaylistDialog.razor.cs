using BeatSaberTools.Core.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

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
                    Type = "RECENTLY_ADDED_MAPS",
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