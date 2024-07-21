using MapMaven.Core.Models.Data.Playlists;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = MapMaven.Models.Playlist;

namespace MapMaven.Components.Playlists
{
    public partial class PlaylistList
    {
        [Inject]
        protected IPlaylistService PlaylistService { get; set; }

        [Inject]
        protected IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        private PlaylistTree<Playlist> PlaylistTree = new();
        private bool LoadingPlaylists = false;

        private Playlist SelectedPlaylist;
        private bool DeleteMaps = false;

        private BehaviorSubject<string> _playlistSearchText = new(string.Empty);

        private string PlaylistSearchText
        {
            get => _playlistSearchText.Value;
            set => _playlistSearchText.OnNext(value);
        }

        private Playlist? PlaylistToDelete = null;
        private bool DeleteDialogVisible = false;
        private bool DeletingPlaylist = false;

        protected override void OnInitialized()
        {
            var playlistTree = Observable.CombineLatest(PlaylistService.PlaylistTree, _playlistSearchText, (playlistTree, searchText) => (playlistTree, searchText));

            SubscribeAndBind(playlistTree, x =>
            {
                var filteredPlaylistTree = new PlaylistTree<Playlist>();
                filteredPlaylistTree.RootPlaylistFolder = FilterPlaylistFolder((PlaylistFolder<Playlist>)x.playlistTree.RootPlaylistFolder.Copy(), x.searchText);

                PlaylistTree = filteredPlaylistTree;
            });

            BeatSaberDataService.LoadingPlaylistInfo.Subscribe(loading =>
            {
                LoadingPlaylists = loading;
                InvokeAsync(StateHasChanged);
            });

            PlaylistService.SelectedPlaylist.Subscribe(playlist =>
            {
                SelectedPlaylist = playlist;
                InvokeAsync(StateHasChanged);
            });
        }

        PlaylistFolder<Playlist> FilterPlaylistFolder(PlaylistFolder<Playlist> playlistFolder, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return playlistFolder;

            playlistFolder.ChildItems = playlistFolder.ChildItems
                .Where(item => PlaylistTreeItemContainsItem(item, searchText))
                .ToList();

            foreach (var item in playlistFolder.ChildItems)
            {
                if (item is PlaylistFolder<Playlist> childFolder)
                {
                    childFolder = FilterPlaylistFolder(childFolder, searchText);
                }
            }

            return playlistFolder;
        }

        bool PlaylistTreeItemContainsItem(PlaylistTreeItem<Playlist> item, string searchText) => item switch
        {
            PlaylistFolder<Playlist> childFolder => childFolder.ChildItems.Any(item => PlaylistTreeItemContainsItem(item, searchText)),
            PlaylistTreeNode<Playlist> playlist => playlist.Playlist.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        protected void OnPlaylistSelect(Playlist playlist)
        {
            if (playlist is null)
                return;

            NavigationManager.NavigateTo("/maps");

            SelectedPlaylist = playlist;

            // Set the selected playlist once the navigation to maps page completes (one time event callback) (prevents costly filter execution on current page)
            NavigationManager.LocationChanged += SetSelectedPlaylistAfterNavigation;
        }

        private void SetSelectedPlaylistAfterNavigation(object sender, LocationChangedEventArgs e)
        {
            PlaylistService.SetSelectedPlaylist(SelectedPlaylist);
            NavigationManager.LocationChanged -= SetSelectedPlaylistAfterNavigation;
        }

        protected void OpenAddPlaylistDialog()
        {
            DialogService.Show<EditPlaylistDialog>("Add playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }
    }
}