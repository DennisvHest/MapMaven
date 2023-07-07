using MapMaven.Core.Services.Interfaces;
using MapMaven.Extensions;
using MapMaven.Models;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Image = System.Drawing.Image;

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
            if (NewPlaylist)
            {
                await PlaylistService.AddPlaylist(EditPlaylistModel);
                Snackbar.Add($"Added playlist \"{EditPlaylistModel.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
            else
            {
                await PlaylistService.EditPlaylist(EditPlaylistModel);
                Snackbar.Add($"Saved playlist \"{EditPlaylistModel.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }

            MudDialog.Close(DialogResult.Ok(EditPlaylistModel));
        }

        void Cancel() => MudDialog.Cancel();
    }
}