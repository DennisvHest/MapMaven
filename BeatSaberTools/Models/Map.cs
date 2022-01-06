using System;

namespace BeatSaberTools.Models
{
    public class Map
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SongAuthorName { get; set; }
        public string MapAuthorName { get; set; }
        public TimeSpan SongDuration { get; set; }
        public TimeSpan PreviewStartTime { get; set; }
        public TimeSpan PreviewDuration { get; set; }
    }
}
