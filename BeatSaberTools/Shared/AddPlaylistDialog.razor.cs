using BeatSaberPlaylistsLib.Types;
using BeatSaberTools.Extensions;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Image = System.Drawing.Image;

namespace BeatSaberTools.Shared
{
    public partial class AddPlaylistDialog
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        AddPlaylistModel AddPlaylistModel = new AddPlaylistModel();

        string CoverImage;

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
                    AddPlaylistModel.CoverImage = coverImage.ToBase64PrependedString();
                }
            }
            else
            {
                CoverImage = null;
                AddPlaylistModel.CoverImage = null;
            }
        }

        async Task OnValidSubmit(EditContext context)
        {
            await PlaylistService.AddPlaylist(AddPlaylistModel);

            Snackbar.Add($"Added playlist \"{AddPlaylistModel.Name}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);

            MudDialog.Close(DialogResult.Ok(AddPlaylistModel));
        }

        void Cancel() => MudDialog.Cancel();


        private static string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mud-width-full mud-height-full d-flex align-center";
        private string DragClass = DefaultDragClass;

        private void SetDragClass()
        {
            DragClass = $"{DefaultDragClass} mud-border-primary";
        }

        private void ClearDragClass()
        {
            DragClass = DefaultDragClass;
        }
    }
}