using MapMaven.Core.Models;
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

        BehaviorSubject<PlaylistType?> _playlistTypeFilter = new(null);

        PlaylistType? PlaylistTypeFilter
        {
            get => _playlistTypeFilter.Value;
            set => _playlistTypeFilter.OnNext(value);
        }

        Playlist? PlaylistToDelete = null;
        bool DeleteDialogVisible = false;
        bool DeletingPlaylist = false;

        protected override void OnInitialized()
        {
            var playlistTree = Observable.CombineLatest(
                PlaylistService.PlaylistTree,
                _playlistSearchText,
                _playlistTypeFilter,
                (playlistTree, searchText, playlistTypeFilter) => (playlistTree, searchText, playlistTypeFilter)
            );

            SubscribeAndBind(playlistTree, x =>
            {
                var filteredPlaylistTree = new PlaylistTree<Playlist>();

                filteredPlaylistTree.RootPlaylistFolder = FilterPlaylistFolder(
                    playlistFolder: (PlaylistFolder<Playlist>)x.playlistTree.RootPlaylistFolder.Copy(),
                    searchText: x.searchText,
                    playlistType: x.playlistTypeFilter
                );

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

        PlaylistFolder<Playlist> FilterPlaylistFolder(PlaylistFolder<Playlist> playlistFolder, string searchText, PlaylistType? playlistType)
        {
            if (string.IsNullOrEmpty(searchText) && playlistType is null)
                return playlistFolder;

            playlistFolder.ChildItems = playlistFolder.ChildItems
                .Where(item => PlaylistTreeItemContainsItem(item, searchText, playlistType))
                .ToList();

            foreach (var item in playlistFolder.ChildItems)
            {
                if (item is PlaylistFolder<Playlist> childFolder)
                {
                    childFolder = FilterPlaylistFolder(childFolder, searchText, playlistType);
                }
            }

            return playlistFolder;
        }

        bool PlaylistTreeItemContainsItem(PlaylistTreeItem<Playlist> item, string searchText, PlaylistType? playlistType) => item switch
        {
            PlaylistFolder<Playlist> childFolder =>
                !string.IsNullOrEmpty(searchText) && childFolder.FolderName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || childFolder.ChildItems.Any(item => PlaylistTreeItemContainsItem(item, searchText, playlistType)),
            PlaylistTreeNode<Playlist> playlist =>
                (string.IsNullOrEmpty(searchText) ||  playlist.Playlist.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                && (
                    playlistType is null
                    || playlistType == PlaylistType.Playlist && !playlist.Playlist.IsDynamicPlaylist
                    || playlistType == PlaylistType.DynamicPlaylist && playlist.Playlist.IsDynamicPlaylist
                ),
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