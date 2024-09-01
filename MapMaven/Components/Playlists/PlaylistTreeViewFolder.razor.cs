using BeatSaberPlaylistsLib;
using MapMaven.Components.Shared;
using MapMaven.Core.Models.Data.Playlists;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Components.Playlists
{
    public partial class PlaylistTreeViewFolder
    {
        [Inject]
        IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        IBeatSaberDataService BeatSaberDataService { get; set; }

        [Parameter]
        public PlaylistFolder<Playlist> Folder { get; set; }

        [Parameter]
        public string MapToAddHash { get; set; }

        [Parameter]
        public bool Expanded { get; set; }

        [Parameter]
        public bool Loading { get; set; }

        Playlist? PlaylistToDelete = null;
        bool DeletePlaylistDialogVisible = false;
        bool DeletingPlaylist = false;
        bool DeleteMaps = false;

        void OpenEditPlaylistDialog(Playlist playlist)
        {
            if (playlist.IsLivePlaylist)
            {
                var parameters = new DialogParameters
                {
                    { "SelectedPlaylist", new EditLivePlaylistModel(playlist) },
                    { "NewPlaylist", false }
                };

                DialogService.Show<EditLivePlaylistDialog>("Edit playlist", parameters, new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                });
            }
            else
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
        }

        void OpenDeletePlaylistDialog(Playlist playlistToDelete)
        {
            PlaylistToDelete = playlistToDelete;
            DeletePlaylistDialogVisible = true;
        }

        void ClosePlaylistDelete()
        {
            PlaylistToDelete = null;
            DeletePlaylistDialogVisible = false;
            DeleteMaps = false;
        }

        async Task DeletePlaylist()
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

        void OpenAddPlaylistDialog(PlaylistFolder<Playlist> playlistFolder)
        {
            DialogService.Show<EditPlaylistDialog>("Add playlist", new DialogParameters
                {
                    { nameof(EditPlaylistDialog.EditPlaylistModel), new EditPlaylistModel { PlaylistManager = playlistFolder.PlaylistManager } }
                },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );
        }

        void OpenAddLivePlaylistDialog(PlaylistFolder<Playlist> playlistFolder)
        {
            DialogService.Show<EditLivePlaylistDialog>("Add playlist", new DialogParameters
                {
                    { nameof(EditLivePlaylistDialog.PlaylistManager), playlistFolder.PlaylistManager }
                },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );
        }

        void OpenAddPlaylistFolderDialog(PlaylistFolder<Playlist> playlistFolder)
        {
            DialogService.Show<EditPlaylistFolderDialog>("Add playlist folder", new DialogParameters
                {
                    { nameof(EditPlaylistFolderDialog.ParentPlaylistManager), playlistFolder.PlaylistManager }
                },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );
        }

        void OpenEditPlaylistFolderDialog(PlaylistFolder<Playlist> playlistFolder)
        {
            DialogService.Show<EditPlaylistFolderDialog>("Add playlist folder", new DialogParameters
                {
                    { nameof(EditPlaylistFolderDialog.ParentPlaylistManager), Folder.PlaylistManager },
                    { nameof(EditPlaylistFolderDialog.PlaylistManager), playlistFolder.PlaylistManager }
                },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                }
            );
        }

        async Task OpenDeletePlaylistFolderDialog(PlaylistFolder<Playlist> playlistFolder)
        {
            var dialog = await DialogService.ShowAsync<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to delete the \"{playlistFolder.FolderName}\" playlist folder? All playlists and folders within this folder will be deleted." },
                { nameof(ConfirmationDialog.ConfirmText), "Delete" }
            });

            var result = await dialog.Result;

            if (result.Canceled)
                return;

            await PlaylistService.DeletePlaylistFolder(playlistFolder!.PlaylistManager);

            Snackbar.Add($"Deleted playlist folder \"{playlistFolder.FolderName}\"", Severity.Normal, config => config.Icon = Icons.Material.Filled.Check);
        }

        int GetLoadedMapsCount(Playlist playlist)
        {
            return playlist.Maps
                .Where(m => BeatSaberDataService.MapIsLoaded(m.Hash))
                .Count();
        }
    }
}