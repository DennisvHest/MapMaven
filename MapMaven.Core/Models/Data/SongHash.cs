using System.Text.Json.Serialization;

namespace BeatSaberTools.Models.Data
{
    public class SongHash
    {
        public long DirectoryHash { get; set; }
        [JsonPropertyName("songHash")]
        public string Hash { get; set; }
    }
}
