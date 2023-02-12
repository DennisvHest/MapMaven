using MapMaven.Models;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components.Playlists
{
    public partial class PlaylistSelector
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        private IEnumerable<Playlist> Playlists = Array.Empty<Playlist>();

        protected override void OnInitialized()
        {
            PlaylistService.Playlists.Subscribe(playlists =>
            {
                Playlists = playlists;
                InvokeAsync(StateHasChanged);
            });
        }

        protected void OnPlaylistSelect(Playlist playlist)
        {
            MudDialog.Close(DialogResult.Ok(playlist));
        }
    }
}