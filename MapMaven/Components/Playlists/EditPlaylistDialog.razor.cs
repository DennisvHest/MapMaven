using MapMaven.Core.Services.Interfaces;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace MapMaven.Components.Playlists
{
    public partial class EditPlaylistDialog
    {
        [Inject]
        protected IPlaylistService PlaylistService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public EditPlaylistModel EditPlaylistModel { get; set; }

        bool NewPlaylist => EditPlaylistModel.FileName == null;

        protected override void OnInitialized()
        {
            if (EditPlaylistModel == null)
                EditPlaylistModel = new EditPlaylistModel();
        }

        async Task OnValidSubmit(EditContext context)
        {
            Playlist playlist;

            if (NewPlaylist)
            {
                playlist = await PlaylistService.AddPlaylist(EditPlaylistModel);
                Snackbar.Add($"Added playlist \"{EditPlaylistModel.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
            else
            {
                playlist = await PlaylistService.EditPlaylist(EditPlaylistModel);
                Snackbar.Add($"Saved playlist \"{EditPlaylistModel.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }

            MudDialog.Close(DialogResult.Ok(playlist));
        }

        void Cancel() => MudDialog.Cancel();
    }
}