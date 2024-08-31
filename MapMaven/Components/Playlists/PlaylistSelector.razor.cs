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

        [Parameter]
        public string MapToAddHash { get; set; }

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
                    playlistType: PlaylistType.Playlist,
                    includeEmptyFolders: true
                );

                PlaylistTree = filteredPlaylistTree;
            });
        }

        protected void OnPlaylistSelect(Playlist? playlist)
        {
            MudDialog.Close(DialogResult.Ok(playlist));
        }

        void OpenAddPlaylistDialog()
        {
            DialogService.Show<EditPlaylistDialog>("Add playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        void OpenAddPlaylistFolderDialog()
        {
            DialogService.Show<EditPlaylistFolderDialog>("Add playlist folder", new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }
    }
}