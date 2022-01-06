using NAudio.Wave;
using System;

namespace BeatSaberTools.Utilities.NAudio
{
    public class StartEndReader : IWaveProvider
    {
        private TimeSpan _end;
        private WaveStream _stream;

        public StartEndReader(WaveStream stream, TimeSpan start, TimeSpan end)
        {
            _stream = stream;
            _stream.CurrentTime = start;
            _end = end;
        }

        public int Read(byte[] array, int offset, int count)
        {
            if (_stream.CurrentTime > _end)
                return 0;

            return _stream.Read(array, offset, count);
        }

        public WaveFormat WaveFormat => _stream.WaveFormat;
    }
}
