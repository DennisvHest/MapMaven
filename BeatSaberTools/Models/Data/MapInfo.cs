using System.Text.Json.Serialization;

namespace BeatSaberTools.Models.Data
{
    public class MapInfo
    {
        [JsonPropertyName("_songName")]
        public string SongName { get; set; }
    }
}
