using System.Text.Json.Serialization;

namespace MapMaven.Models.Data
{
    public class MapInfo
    {
        public string Id { get; set; }
        public string Hash { get; set; }
        public string DirectoryPath { get; set; }

        [JsonPropertyName("_songName")]
        public string SongName { get; set; }
        [JsonPropertyName("_songAuthorName")]
        public string SongAuthorName { get; set; }
        [JsonPropertyName("_levelAuthorName")]
        public string LevelAuthorName { get; set; }
        [JsonPropertyName("_songFilename")]
        public string SongFileName { get; set; }
        [JsonPropertyName("_coverImageFilename")]
        public string CoverImageFilename { get; set; }
        public DateTimeOffset AddedDateTime { get; set; }
        public TimeSpan SongDuration { get; set; }
        [JsonPropertyName("_previewStartTime")]
        public float PreviewStartTimeInSeconds { get; set; }
        [JsonPropertyName("_previewDuration")]
        public float PreviewDurationInSeconds { get; set; }

        [JsonIgnore]
        public TimeSpan PreviewStartTime => TimeSpan.FromSeconds(PreviewStartTimeInSeconds);

        [JsonIgnore]
        public TimeSpan PreviewDuration
        {
            get
            {
                try
                {
                    return TimeSpan.FromSeconds(PreviewDurationInSeconds);
                }
                catch
                {
                    return SongDuration - PreviewStartTime;
                }
            }
        }

        public Map ToMap()
        {
            return new Map
            {
                Id = Id,
                Hash = Hash,
                Name = SongName,
                SongAuthorName = SongAuthorName,
                MapAuthorName = LevelAuthorName,
                AddedDateTime = AddedDateTime,
                SongDuration = SongDuration,
                PreviewStartTime = PreviewStartTime,
                PreviewDuration = PreviewDuration
            };
        }
    }
}
