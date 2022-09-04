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

        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

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

        async Task AddDynamicPlaylist(EditDynamicPlaylistModel dynamicPlaylist)
        {
            await PlaylistService.AddDynamicPlaylist(dynamicPlaylist);
        }

        void Cancel() => MudDialog.Cancel();
    }
}