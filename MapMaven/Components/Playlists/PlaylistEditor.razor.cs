using MapMaven.Extensions;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Image = System.Drawing.Image;

namespace MapMaven.Components.Playlists
{
    public partial class PlaylistEditor
    {
        [Parameter]
        public EditPlaylistModel EditPlaylistModel { get; set; }

        protected override void OnInitialized()
        {
            if (EditPlaylistModel == null)
                EditPlaylistModel = new EditPlaylistModel();
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
                    var coverImageBase64 = coverImage.ToDataUrl();
                    EditPlaylistModel.CoverImage = coverImageBase64;
                }
            }
            else
            {
                EditPlaylistModel.CoverImage = null;
            }
        }
    }
}