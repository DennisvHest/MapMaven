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
        private BehaviorSubject<string> _dynamicPlaylistSearchText = new(string.Empty);

        private string PlaylistSearchText
        {
            get => _playlistSearchText.Value;
            set => _playlistSearchText.OnNext(value);
        }

        private string DynamicPlaylistSearchText
        {
            get => _dynamicPlaylistSearchText.Value;
            set => _dynamicPlaylistSearchText.OnNext(value);
        }

        private Playlist? PlaylistToDelete = null;
        private bool DeleteDialogVisible = false;
        private bool DeletingPlaylist = false;

        protected override void OnInitialized()
        {
            SubscribeAndBind(PlaylistService.PlaylistTree, playlistTree => PlaylistTree = playlistTree);

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