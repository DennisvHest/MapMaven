using Microsoft.AspNetCore.Components;
using BeatSaberTools.Services;
using BeatSaberTools.Extensions;
using System.Reactive.Linq;
using Map = BeatSaberTools.Models.Map;
using MudBlazor;
using BeatSaberTools.Shared;
using BeatSaberTools.Models;
using BeatSaberTools.Core.Services;

namespace BeatSaberTools.Pages
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

        protected string CoverImageDataUrl { get; set; }

        bool ShowConfirmRemoveFromPlaylist = false;

        IDisposable SelectedPlaylistSubscription;

        protected override void OnInitialized()
        {
            Task.Run(() =>
            {
                var coverImage = BeatSaberDataService.GetMapCoverImage(Map.Hash);

                CoverImageDataUrl = coverImage
                    .GetResizedImage(50, 50)
                    .ToDataUrl();

                InvokeAsync(StateHasChanged);
            });

            var currentlyPlaying = SongPlayerService.CurrentlyPlayingMap
                .Select(m => m != null && m.Hash == Map?.Hash);

            SubscribeAndBind(currentlyPlaying, currentlyPlaying => CurrentlyPlaying = currentlyPlaying);

            var playbackProgress = Observable.CombineLatest(currentlyPlaying, SongPlayerService.PlaybackProgress, (playing, progress) => (playing, progress))
                .Where(x => x.playing);

            SubscribeAndBind(playbackProgress, x => PlaybackProgress = x.progress);

            SelectedPlaylistSubscription = SubscribeAndBind(PlaylistService.SelectedPlaylist, selectedPlaylist => SelectedPlaylist = selectedPlaylist);
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

        void OpenDeleteFromPlaylistDialog()
        {
            ShowConfirmRemoveFromPlaylist = true;
        }

        void CloseDeleteFromPlaylistDialog()
        {
            ShowConfirmRemoveFromPlaylist = false;
        }

        async Task RemoveFromPlaylist()
        {
            await PlaylistService.RemoveMapFromPlaylist(Map, SelectedPlaylist);

            Snackbar.Add($"Removed map \"{Map.Name}\" from playlist \"{SelectedPlaylist.Title}\"", Severity.Normal, config => config.Icon = Icons.Filled.Check);

            CloseDeleteFromPlaylistDialog();
            InvokeAsync(() => StateHasChanged());
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
            MapService.SelectSongAuthor(Map.SongAuthorName);
        }

        public void Dispose()
        {
            SelectedPlaylistSubscription?.Dispose();
        }
    }
}