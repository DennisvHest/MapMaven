using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reactive.Linq;
using Playlist = BeatSaberTools.Models.Playlist;

namespace BeatSaberTools.Components.Playlists
{
    public partial class PlaylistList
    {
        [Inject]
        protected PlaylistService PlaylistService { get; set; }

        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        private IEnumerable<Playlist> Playlists = Array.Empty<Playlist>();
        private IEnumerable<Playlist> DynamicPlaylists = Array.Empty<Playlist>();
        private bool LoadingPlaylists = false;

        private Playlist SelectedPlaylist;
        private object SelectedPlaylistValue;

        private Playlist PlaylistToDelete;

        bool ShowConfirmDelete = false;

        protected override void OnInitialized()
        {
            var allPlaylists = PlaylistService.Playlists;

            var playlists = allPlaylists
                .Select(playlists => playlists.Where(p => !p.IsDynamicPlaylist));

            var dynamicPlaylists = allPlaylists
                .Select(playlists => playlists.Where(p => p.IsDynamicPlaylist));

            playlists.Subscribe(playlists =>
            {
                Playlists = playlists;
                InvokeAsync(StateHasChanged);
            });

            dynamicPlaylists.Subscribe(playlists =>
            {
                DynamicPlaylists = playlists;
                InvokeAsync(StateHasChanged);
            });

            BeatSaberDataService.LoadingPlaylistInfo.Subscribe(loading =>
            {
                LoadingPlaylists = loading;
                InvokeAsync(StateHasChanged);
            });

            PlaylistService.SelectedPlaylist.Subscribe(playlist =>
            {
                SelectedPlaylist = playlist;
                SelectedPlaylistValue = SelectedPlaylist;
                InvokeAsync(StateHasChanged);
            });
        }

        protected void OnPlaylistSelect(Playlist playlist)
        {
            PlaylistService.SetSelectedPlaylist(playlist);
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
            var parameters = new DialogParameters();
            parameters.Add("EditPlaylistModel", new EditPlaylistModel(playlist));

            DialogService.Show<EditPlaylistDialog>("Edit playlist", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        protected void OpenDeletePlaylistDialog(Playlist playlistToDelete)
        {
            PlaylistToDelete = playlistToDelete;
            ShowConfirmDelete = true;
        }

        protected async Task DeletePlaylist()
        {
            await PlaylistService.DeletePlaylist(PlaylistToDelete);
            
            Snackbar.Add($"Removed playlist \"{PlaylistToDelete.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            
            RemovePlaylistToDelete();
        }

        protected void RemovePlaylistToDelete()
        {
            PlaylistToDelete = null;
            ShowConfirmDelete = false;
        }
    }
}