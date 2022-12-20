using BeatSaberTools.Extensions;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Image = System.Drawing.Image;

namespace BeatSaberTools.Components.Playlists
{
    public partial class EditPlaylistDialog
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Parameter]
        public EditPlaylistModel EditPlaylistModel { get; set; }

        bool NewPlaylist => EditPlaylistModel.FileName == null;

        string CoverImage;

        protected override void OnInitialized()
        {
            if (EditPlaylistModel == null)
                EditPlaylistModel = new EditPlaylistModel();
        }

        protected override void OnParametersSet()
        {
            CoverImage = EditPlaylistModel.CoverImage;
        }

        private async Task OnInputFileChanged(InputFileChangeEventArgs e)
        {
            if (e.File != null)
            {
                var imageFile = e.File.OpenReadStream(maxAllowedSize: long.MaxValue);

                using (var ms = new MemoryStream())
                {
                    await imageFile.CopyToAsync(ms);
                    var coverImage = Image.FromStream(ms);
                    CoverImage = coverImage.ToDataUrl();
                    EditPlaylistModel.CoverImage = coverImage.ToBase64PrependedString();
                }
            }
            else
            {
                CoverImage = null;
                EditPlaylistModel.CoverImage = null;
            }
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