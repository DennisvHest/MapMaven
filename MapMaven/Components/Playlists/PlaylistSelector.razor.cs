using MapMaven.Core.Services.Interfaces;
using MapMaven.Models;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Components.Playlists
{
    public partial class PlaylistSelector
    {
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; }

        [Inject]
        protected IPlaylistService PlaylistService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        private IEnumerable<Playlist> Playlists = Array.Empty<Playlist>();

        private BehaviorSubject<string> _playlistSearchText = new(string.Empty);

        private string PlaylistSearchText
        {
            get => _playlistSearchText.Value;
            set => _playlistSearchText.OnNext(value);
        }

        protected override void OnInitialized()
        {
            var playlists = PlaylistService.Playlists
                .Select(playlists => playlists.Where(p => !p.IsDynamicPlaylist));

            SubscribeAndBind(Observable.CombineLatest(playlists, _playlistSearchText, (playlists, searchText) =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return playlists;

                return playlists
                    .Where(p => p.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }), playlists => Playlists = playlists);
        }

        protected void OnPlaylistSelect(Playlist? playlist)
        {
            MudDialog.Close(DialogResult.Ok(playlist));
        }

        protected async Task CreateNewPlaylist()
        {
            var editPlaylistDialog = DialogService.Show<EditPlaylistDialog>("Add playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });

            var result = await editPlaylistDialog.Result;

            if (!result.Canceled)
                MudDialog.Close(DialogResult.Ok((Playlist)result.Data));
        }
    }
}