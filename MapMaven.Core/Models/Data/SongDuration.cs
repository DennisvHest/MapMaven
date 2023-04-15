using System.Text.Json.Serialization;

namespace MapMaven.Core.Models.Data
{
    public class SongDuration
    {
        public string Id { get; set; }
        [JsonPropertyName("duration")]
        public double DurationInSeconds { get; set; }
    }
}
