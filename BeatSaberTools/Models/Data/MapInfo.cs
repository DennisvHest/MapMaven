using System.Text.Json.Serialization;

namespace BeatSaberTools.Models.Data
{
    public class MapInfo
    {
        public string Id { get; set; }
        public string DirectoryPath { get; set; }

        [JsonPropertyName("_songName")]
        public string SongName { get; set; }
        [JsonPropertyName("_coverImageFilename")]
        public string CoverImageFilename { get; set; }

        public Map ToMap()
        {
            return new Map
            {
                Id = Id,
                Name = SongName
            };
        }
    }
}
