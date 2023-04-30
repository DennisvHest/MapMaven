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
        protected PlaylistService PlaylistService { get; set; }

        private IEnumerable<Playlist> Playlists = Array.Empty<Playlist>();

        private BehaviorSubject<string> _playlistSearchText = new(string.Empty);

        private string PlaylistSearchText
        {
            get => _playlistSearchText.Value;
            set => _playlistSearchText.OnNext(value);
        }

        protected override void OnInitialized()
        {
            SubscribeAndBind(Observable.CombineLatest(PlaylistService.Playlists, _playlistSearchText, (playlists, searchText) =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return playlists;

                return playlists
                    .Where(p => p.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }), playlists => Playlists = playlists);
        }

        protected void OnPlaylistSelect(Playlist playlist)
        {
            MudDialog.Close(DialogResult.Ok(playlist));
        }
    }
}