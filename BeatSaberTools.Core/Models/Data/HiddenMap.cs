namespace BeatSaberTools.Core.Models.Data
{
    public class HiddenMap
    {
        public Guid Id { get; set; }
        public string Hash { get; set; }
        public string? Difficulty { get; set; }
        public string PlayerId { get; set; }

        public virtual Player Player { get; set; }
    }
}
