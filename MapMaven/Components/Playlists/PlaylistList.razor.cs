using MapMaven.Core.Models.Data.Playlists;
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
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        NavigationManager NavigationManager { get; set; }

        PlaylistTree<Playlist> PlaylistTree = new();
        bool LoadingPlaylists = false;

        Playlist SelectedPlaylist;
        bool DeleteMaps = false;

        BehaviorSubject<string> _playlistSearchText = new(string.Empty);

        string PlaylistSearchText
        {
            get => _playlistSearchText.Value;
            set => _playlistSearchText.OnNext(value);
        }

        Playlist? PlaylistToDelete = null;
        bool DeleteDialogVisible = false;
        bool DeletingPlaylist = false;

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

        void OnPlaylistSelect(Playlist playlist)
        {
            if (playlist is null)
                return;

            NavigationManager.NavigateTo("/maps");

            SelectedPlaylist = playlist;

            // Set the selected playlist once the navigation to maps page completes (one time event callback) (prevents costly filter execution on current page)
            NavigationManager.LocationChanged += SetSelectedPlaylistAfterNavigation;
        }

        void SetSelectedPlaylistAfterNavigation(object sender, LocationChangedEventArgs e)
        {
            PlaylistService.SetSelectedPlaylist(SelectedPlaylist);
            NavigationManager.LocationChanged -= SetSelectedPlaylistAfterNavigation;
        }

        void OpenAddPlaylistDialog()
        {
            DialogService.Show<EditPlaylistDialog>("Add playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        void OpenAddDynamicPlaylistDialog()
        {
            DialogService.Show<EditDynamicPlaylistDialog>("Add playlist", new DialogOptions
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