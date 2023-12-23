using Microsoft.AspNetCore.Components;
using System.Reactive.Linq;
using Map = MapMaven.Models.Map;
using MudBlazor;
using MapMaven.Models;
using MapMaven.Components.Playlists;
using MapMaven.Components.Shared;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Models;

namespace MapMaven.Components.Maps
{
    public partial class MapBrowserRow : IDisposable
    {
        [Inject]
        protected IPlaylistService PlaylistService { get; set; }
        [Inject]
        protected ILeaderboardService ScoreSaberService { get; set; }
        [Inject]
        protected IMapService MapService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        protected Playlist SelectedPlaylist { get; set; }

        protected string CoverImageUrl { get; set; }

        public string? PlayerId { get; set; } = null;
        private bool Selectable = false;

        IDisposable SelectedPlaylistSubscription;

        protected override void OnInitialized()
        {
            SelectedPlaylistSubscription = SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist => SelectedPlaylist = selectedPlaylist);

            SubscribeAndBind(ScoreSaberService.PlayerIdObservable, playerId => PlayerId = playerId);
            SubscribeAndBind(MapService.Selectable, selectable => Selectable = selectable);
        }

        async Task OpenAddMapToPlaylistDialog(Map map)
        {
            var dialog = await DialogService.ShowAsync<PlaylistSelector>("Add map to playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                CloseButton = true
            });

            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                var playlist = (Playlist)result.Data;

                await PlaylistService.AddMapToPlaylist(map, playlist);

                Snackbar.Add($"Added map \"{map.Name}\" to \"{playlist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
        }

        async Task OpenDeleteFromPlaylistDialog(Map map)
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to remove \"{map.Name}\" from the \"{SelectedPlaylist.Title}\" playlist?" },
                { nameof(ConfirmationDialog.ConfirmText), $"Remove" }
            });

            var result = await dialog.Result;

            if (!result.Cancelled)
                await RemoveFromPlaylist(map);
        }

        async Task RemoveFromPlaylist(Map map)
        {
            await PlaylistService.RemoveMapFromPlaylist(map, SelectedPlaylist);

            Snackbar.Add($"Removed map \"{map.Name}\" from playlist \"{SelectedPlaylist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
        }

        void OpenReplay(Map map, PlayerScore playerScore)
        {
            var parameters = new DialogParameters
            {
                { nameof(Replay.MapId), map.Id },
                { nameof(Replay.PlayerScore), playerScore },
            };

            DialogService.Show<Replay>(null, parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                CloseButton = true
            });
        }

        void SelectSongAuthor(Map map)
        {
            MapService.AddMapFilter(new MapFilter
            {
                Name = map.SongAuthorName,
                Filter = otherMap => map.SongAuthorName == otherMap.SongAuthorName
            });
        }

        void OpenDetails(Map map)
        {
            DialogService.Show<MapDetail>(
                title: null,
                parameters: new() { { nameof(MapDetail.Map), map } },
                options: new()
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    CloseButton = true
                }
            );
        }

        async Task DeleteMap(Map map)
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to delete \"{map.Name}\" from the game? This cannot be undone." },
                { nameof(ConfirmationDialog.ConfirmText), $"Delete" }
            });

            var result = await dialog.Result;

            if (result.Cancelled)
                return;

            await MapService.DeleteMap(map.Hash);
        }

        public void Dispose()
        {
            SelectedPlaylistSubscription?.Dispose();
        }
    }
}