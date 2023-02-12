using MapMaven.Models;

namespace MapMaven.Core.Models.Data
{
    public class HiddenMap
    {
        public Guid Id { get; set; }
        public string Hash { get; set; }
        public string? Difficulty { get; set; }
        public string PlayerId { get; set; }

        public virtual Player Player { get; set; }

        public HiddenMap() { }

        public HiddenMap(Map map)
        {
            Hash = map.Hash;
            Difficulty = map.ScoreEstimates.SingleOrDefault()?.Difficulty;
        }
    }
}
