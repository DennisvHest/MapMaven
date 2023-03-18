using MapMaven.Components.Shared;
using MapMaven.Models;
using MapMaven.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reactive.Linq;
using Playlist = MapMaven.Models.Playlist;

namespace MapMaven.Components.Playlists
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
                SelectedPlaylistValue = SelectedPlaylist?.FileName;
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

        protected async Task OpenDeletePlaylistDialog(Playlist playlistToDelete)
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to delete the \"{playlistToDelete.Title}\" playlist?" },
                { nameof(ConfirmationDialog.ConfirmText), $"Delete" }
            });

            var result = await dialog.Result;

            if (!result.Cancelled)
                await DeletePlaylist(playlistToDelete);
        }

        protected async Task DeletePlaylist(Playlist playlistToDelete)
        {
            await PlaylistService.DeletePlaylist(playlistToDelete);
            
            Snackbar.Add($"Removed playlist \"{playlistToDelete.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
        }
    }
}