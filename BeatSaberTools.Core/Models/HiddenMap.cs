using BeatSaberTools.Models;

namespace BeatSaberTools.Core.Models
{
    public class HiddenMap
    {
        public string Hash { get; set; }
        public string? Difficulty { get; set; }

        public HiddenMap() { }

        public HiddenMap(Map map)
        {
            Hash = map.Hash;
            Difficulty = map.ScoreEstimate.SingleOrDefault()?.Difficulty;
        }

        public override bool Equals(object? obj)
        {
            var otherMap = obj as HiddenMap;

            return Hash == otherMap?.Hash
                && Difficulty == otherMap?.Difficulty;
        }

        public override int GetHashCode()
        {
            return (Hash + Difficulty).GetHashCode();
        }
    }
}
