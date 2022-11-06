
using System.Text.Json.Serialization;

namespace BeatSaberTools.Core.Models.Data.ScoreSaber
{
    public class RankedMap
    {
        public long Uid { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Mapper { get; set; }
        public decimal Bpm { get; set; }
        [JsonPropertyName("diff")]
        public string Difficulty { get; set; }
        public int Scores { get; set; }
        public decimal Stars { get; set; }
        public decimal PP { get; set; }
        [JsonPropertyName("beatSaverKey")]
        public string Key { get; set; }
        public int Downloads { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public decimal Rating { get; set; }
        public string Download { get; set; }
        public decimal Duration { get; set; }
        public int NoteCount { get; set; }
        [JsonPropertyName("njs")]
        public decimal NoteJumpSpeed { get; set; }
        public int RecentScores { get; set; }
    }
}
