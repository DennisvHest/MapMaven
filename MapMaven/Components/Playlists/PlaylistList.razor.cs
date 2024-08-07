using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Models;
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

        private IEnumerable<Playlist> Playlists = Array.Empty<Playlist>();
        private IEnumerable<Playlist> DynamicPlaylists = Array.Empty<Playlist>();
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
            var allPlaylists = PlaylistService.Playlists;

            var playlists = allPlaylists
                .Select(playlists => playlists.Where(p => !p.IsDynamicPlaylist));

            var dynamicPlaylists = allPlaylists
                .Select(playlists => playlists.Where(p => p.IsDynamicPlaylist));

            SubscribeAndBind(Observable.CombineLatest(playlists, _playlistSearchText, (playlists, searchText) =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return playlists;

                return playlists
                    .Where(p => p.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }), playlists => Playlists = playlists);

            SubscribeAndBind(Observable.CombineLatest(dynamicPlaylists, _dynamicPlaylistSearchText, (dynamicPlaylists, searchText) =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return dynamicPlaylists;

                return dynamicPlaylists
                    .Where(p => p.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }), dynamicPlaylists => DynamicPlaylists = dynamicPlaylists);

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

        protected void OpenAddDynamicPlaylistDialog()
        {
            DialogService.Show<EditDynamicPlaylistDialog>("Add playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        protected void OpenEditPlaylistDialog(Playlist playlist)
        {
            var parameters = new DialogParameters
            {
                { "EditPlaylistModel", new EditPlaylistModel(playlist) }
            };

            DialogService.Show<EditPlaylistDialog>("Edit playlist", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        protected void OpenEditDynamicPlaylistDialog(Playlist playlist)
        {
            var parameters = new DialogParameters
            {
                { "SelectedPlaylist", new EditDynamicPlaylistModel(playlist) },
                { "NewPlaylist", false }
            };

            DialogService.Show<EditDynamicPlaylistDialog>("Edit playlist", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        protected void OpenDeletePlaylistDialog(Playlist playlistToDelete)
        {
            PlaylistToDelete = playlistToDelete;
            DeleteDialogVisible = true;
        }

        protected void ClosePlaylistDelete()
        {
            PlaylistToDelete = null;
            DeleteDialogVisible = false;
            DeleteMaps = false;
        }

        protected async Task DeletePlaylist()
        {
            DeletingPlaylist = true;

            try
            {
                await PlaylistService.DeletePlaylist(PlaylistToDelete, DeleteMaps);

                Snackbar.Add($"Removed playlist \"{PlaylistToDelete.Title}\"", Severity.Normal, config => config.Icon = Icons.Material.Filled.Check);

                ClosePlaylistDelete();
            }
            finally
            {
                DeletingPlaylist = false;
            }
        }

        protected int GetLoadedMapsCount(Playlist playlist)
        {
            return playlist.Maps
                .Where(m => BeatSaberDataService.MapIsLoaded(m.Hash))
                .Count();
        }
    }
}