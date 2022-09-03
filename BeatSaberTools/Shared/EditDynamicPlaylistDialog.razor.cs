using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BeatSaberTools.Shared
{
    public partial class EditDynamicPlaylistDialog
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        async Task AddRecentlyAddedMapsPlaylist()
        {
            await PlaylistService.AddDynamicPlaylist(new EditPlaylistModel
            {
                Name = "RECENTLY_ADDED_MAPS"
            });
        }

        void Cancel() => MudDialog.Cancel();
    }
}