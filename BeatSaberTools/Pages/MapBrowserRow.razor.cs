using Microsoft.AspNetCore.Components;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using BeatSaberTools.Extensions;
using System.Reactive.Linq;
using System;
using System.Threading.Tasks;

namespace BeatSaberTools.Pages
{
    public partial class MapBrowserRow
    {
        [Inject]
        protected BeatSaberDataService BeatSaberDataService { get; set; }
        [Inject]
        protected SongPlayerService SongPlayerService { get; set; }

        [Parameter]
        public Map Map { get; set; }

        private bool CurrentlyPlaying = false;
        private double PlaybackProgress = 0;

        protected string CoverImageDataUrl { get; set; }

        protected override void OnInitialized()
        {
            Task.Run(() =>
            {
                var coverImage = BeatSaberDataService.GetMapCoverImage(Map.Id);

                CoverImageDataUrl = coverImage
                    .GetResizedImage(50, 50)
                    .ToDataUrl();

                InvokeAsync(StateHasChanged);
            });

            var currentlyPlaying = SongPlayerService.CurrentlyPlayingMap
                .Select(m => m != null && m.Id == Map?.Id);

            currentlyPlaying.Subscribe(currentlyPlaying =>
            {
                CurrentlyPlaying = currentlyPlaying;
                StateHasChanged();
            });

            Observable.CombineLatest(currentlyPlaying, SongPlayerService.PlaybackProgress, (playing, progress) => (playing, progress))
                .Where(x => x.playing)
                .Subscribe(x =>
                {
                    PlaybackProgress = x.progress;
                    InvokeAsync(() => StateHasChanged());
                });
        }

        void PlayPauseSongPreview()
        {
            SongPlayerService.PlayStopSongPreview(Map);
        }
    }
}