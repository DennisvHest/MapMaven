using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Playlists;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Models;
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

        [Parameter]
        public bool SaveNewPlaylistOnSubmit { get; set; } = true;

        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        PlaylistTree<Playlist> PlaylistTree = new();

        BehaviorSubject<string> _playlistSearchText = new(string.Empty);

        MudTreeView<Playlist> PlaylistTreeView;

        private string PlaylistSearchText
        {
            get => _playlistSearchText.Value;
            set => _playlistSearchText.OnNext(value);
        }

        protected override void OnInitialized()
        {
            var playlistTree = Observable.CombineLatest(
                PlaylistService.PlaylistTree,
                _playlistSearchText,
                (playlistTree, searchText) => (playlistTree, searchText)
            );

            SubscribeAndBind(playlistTree, x =>
            {
                var filteredPlaylistTree = new PlaylistTree<Playlist>();

                filteredPlaylistTree.RootPlaylistFolder = Services.PlaylistService.FilterPlaylistFolder(
                    playlistFolder: (PlaylistFolder<Playlist>)x.playlistTree.RootPlaylistFolder.Copy(),
                    searchText: x.searchText,
                    playlistType: PlaylistType.Playlist
                );

                PlaylistTree = filteredPlaylistTree;
            });
        }

        protected void OnPlaylistSelect(Playlist? playlist)
        {
            MudDialog.Close(DialogResult.Ok(playlist));
        }

        protected async Task CreateNewPlaylist()
        {
            var editPlaylistDialog = DialogService.Show<EditPlaylistDialog>("Add playlist", new DialogParameters
            {
                { nameof(EditPlaylistDialog.SavePlaylistOnSubmit), SaveNewPlaylistOnSubmit }
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });

            var result = await editPlaylistDialog.Result;

            if (!result.Canceled)
                MudDialog.Close(DialogResult.Ok(result.Data));
        }
    }
}