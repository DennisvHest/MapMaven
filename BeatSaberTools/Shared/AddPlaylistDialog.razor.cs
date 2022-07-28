using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace BeatSaberTools.Shared
{
    public partial class AddPlaylistDialog
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        AddPlaylistModel AddPlaylistModel = new AddPlaylistModel();

        async Task OnValidSubmit(EditContext context)
        {
            await PlaylistService.AddPlaylist(AddPlaylistModel);

            MudDialog.Close(DialogResult.Ok(AddPlaylistModel));
        }

        void Cancel() => MudDialog.Cancel();
    }
}