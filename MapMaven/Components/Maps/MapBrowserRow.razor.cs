using Microsoft.AspNetCore.Components;
using MapMaven.Services;
using MapMaven.Extensions;
using System.Reactive.Linq;
using Map = MapMaven.Models.Map;
using MudBlazor;
using MapMaven.Models;
using MapMaven.Core.Services;
using MapMaven.Components.Playlists;
using MapMaven.Components.Shared;

namespace MapMaven.Components.Maps
{
    public partial class MapBrowserRow : IDisposable
    {
        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }
        [Inject]
        protected SongPlayerService SongPlayerService { get; set; }
        [Inject]
        protected PlaylistService PlaylistService { get; set; }
        [Inject]
        protected ScoreSaberService ScoreSaberService { get; set; }
        [Inject]
        protected MapService MapService { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Parameter]
        public Map Map { get; set; }

        protected Playlist SelectedPlaylist { get; set; }

        private bool CurrentlyPlaying = false;
        private double PlaybackProgress = 0;

        protected string CoverImageUrl { get; set; }

        public string? PlayerId { get; set; } = null;

        IDisposable SelectedPlaylistSubscription;

        protected override void OnInitialized()
        {
            if (string.IsNullOrEmpty(Map.CoverImageUrl))
            {
                Task.Run(() =>
                {
                    var coverImage = BeatSaberDataService.GetMapCoverImage(Map.Hash);

                    CoverImageUrl = coverImage
                        .GetResizedImage(50, 50)
                        .ToDataUrl();

                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                CoverImageUrl = Map.CoverImageUrl;
            }

            var currentlyPlaying = SongPlayerService.CurrentlyPlayingMap
                .Select(m => m != null && m.Hash == Map?.Hash);

            SubscribeAndBind(currentlyPlaying, currentlyPlaying => CurrentlyPlaying = currentlyPlaying);

            var playbackProgress = currentlyPlaying.CombineLatest(SongPlayerService.PlaybackProgress, (playing, progress) => (playing, progress))
                .Where(x => x.playing);

            SubscribeAndBind(playbackProgress, x => PlaybackProgress = x.progress);

            SelectedPlaylistSubscription = SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist => SelectedPlaylist = selectedPlaylist);

            SubscribeAndBind(ScoreSaberService.PlayerIdObservable, playerId => PlayerId = playerId);
        }

        void PlayPauseSongPreview()
        {
            SongPlayerService.PlayStopSongPreview(Map);
        }

        async Task OpenAddMapToPlaylistDialog()
        {
            var dialog = DialogService.Show<PlaylistSelector>("Add map to playlist", new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                CloseButton = true
            });

            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                var playlist = (Playlist)result.Data;

                await PlaylistService.AddMapToPlaylist(Map, playlist);

                Snackbar.Add($"Added map \"{Map.Name}\" to \"{playlist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
            }
        }

        async Task OpenDeleteFromPlaylistDialog()
        {
            var dialog = DialogService.Show<ConfirmationDialog>(null, new DialogParameters
            {
                { nameof(ConfirmationDialog.DialogText), $"Are you sure you want to remove \"{Map.Name}\" from the \"{SelectedPlaylist.Title}\" playlist?" },
                { nameof(ConfirmationDialog.ConfirmText), $"Remove" }
            });

            var result = await dialog.Result;

            if (!result.Cancelled)
                await RemoveFromPlaylist();
        }

        async Task RemoveFromPlaylist()
        {
            await PlaylistService.RemoveMapFromPlaylist(Map, SelectedPlaylist);

            Snackbar.Add($"Removed map \"{Map.Name}\" from playlist \"{SelectedPlaylist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);
        }

        void OpenReplay()
        {
            var parameters = new DialogParameters
            {
                { nameof(Replay.Map), Map }
            };

            DialogService.Show<Replay>(null, parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                CloseButton = true
            });
        }

        void SelectSongAuthor()
        {
            MapService.AddMapFilter(new Core.Models.MapFilter
            {
                Name = Map.SongAuthorName,
                Filter = map => map.SongAuthorName == Map.SongAuthorName
            });
        }

        public void Dispose()
        {
            SelectedPlaylistSubscription?.Dispose();
        }
    }
}