using BeatSaberPlaylistsLib;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Extensions;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Image = System.Drawing.Image;

namespace MapMaven.Components.Playlists
{
    public partial class PlaylistEditor
    {
        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        BeatSaberFileService BeatSaberFileService { get; set; }

        [Parameter]
        public EditPlaylistModel EditPlaylistModel { get; set; }

        IEnumerable<PlaylistManager> PlaylistManagers { get; set; }

        protected override void OnInitialized()
        {
            if (EditPlaylistModel == null)
                EditPlaylistModel = new EditPlaylistModel();

            if (EditPlaylistModel.PlaylistManager is null)
                EditPlaylistModel.PlaylistManager = PlaylistService.GetRootPlaylistManager();

            PlaylistManagers = PlaylistService.GetAllPlaylistManagers();
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