using MapMaven.Core.Services.Interfaces;
using MapMaven.Utilities.NAudio;
using NAudio.Vorbis;
using NAudio.Wave;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = MapMaven.Models.Map;

namespace MapMaven.Services
{
    public class SongPlayerService
    {
        private readonly IBeatSaberDataService _beatSaberDataService;

        private VorbisWaveReader _audioFile;
        private WaveOutEvent _outputDevice;

        private readonly BehaviorSubject<Map> _currentlyPlayingMap = new(null);

        public IObservable<Map> CurrentlyPlayingMap => _currentlyPlayingMap;
        public IObservable<double> PlaybackProgress;

        public SongPlayerService(IBeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;
            InitializePlaybackProgressObservable();
        }

        public void PlayStopSongPreview(Map map)
        {
            if (_outputDevice == null)
                InitializeOutputDevice();

            _outputDevice.Stop();

            if (map.Hash == _currentlyPlayingMap.Value?.Hash)
            {
                _currentlyPlayingMap.OnNext(null);
                return;
            }

            var songPath = _beatSaberDataService.GetMapSongPath(map.Hash);

            _audioFile = new VorbisWaveReader(songPath);

            _audioFile.CurrentTime = map.PreviewStartTime;

            _outputDevice.Init(new StartEndReader(
                _audioFile,
                start: map.PreviewStartTime,
                end: map.PreviewStartTime + map.PreviewDuration
            ));

            _outputDevice.Play();

            _currentlyPlayingMap.OnNext(map);
        }

        private void InitializeOutputDevice()
        {
            _outputDevice = new();

            _outputDevice.PlaybackStopped += (object sender, StoppedEventArgs args) =>
            {
                if (_outputDevice.PlaybackState == PlaybackState.Stopped)
                    _currentlyPlayingMap.OnNext(null);
            };
        }

        private void InitializePlaybackProgressObservable()
        {
            PlaybackProgress = _currentlyPlayingMap.Select(currentlyPlayingMap =>
            {
                if (currentlyPlayingMap == null)
                {
                    return Observable.Empty<double>();
                }
                else
                {
                    return Observable
                        .Interval(TimeSpan.FromMilliseconds(200))
                        .Select(x => (_audioFile.CurrentTime - currentlyPlayingMap.PreviewStartTime) / (currentlyPlayingMap.PreviewEndTime - currentlyPlayingMap.PreviewStartTime));
                }
            }).Switch();
        }
    }
}
