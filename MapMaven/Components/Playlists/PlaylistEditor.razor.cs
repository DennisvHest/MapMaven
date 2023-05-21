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
    }
}