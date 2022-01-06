using BeatSaberTools.Models;
using BeatSaberTools.Utilities.NAudio;
using NAudio.Vorbis;
using NAudio.Wave;
using System.Reactive.Subjects;

namespace BeatSaberTools.Services
{
    public class SongPlayerService
    {
        private readonly BeatSaberDataService _beatSaberDataService;

        private VorbisWaveReader _audioFile;
        private WaveOutEvent _outputDevice;

        private readonly BehaviorSubject<Map> _currentlyPlayingMap = new(null);

        public SongPlayerService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;
        }

        public void PlayStopSongPreview(Map map)
        {
            if (_outputDevice == null)
                _outputDevice = new();

            _outputDevice.Stop();

            if (map.Id == _currentlyPlayingMap.Value?.Id)
            {
                _currentlyPlayingMap.OnNext(null);
                return;
            }

            var songPath = _beatSaberDataService.GetMapSongPath(map.Id);

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
    }
}
