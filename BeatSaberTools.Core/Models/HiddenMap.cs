using BeatSaberTools.Models;
using System.Diagnostics.CodeAnalysis;

namespace BeatSaberTools.Core.Models
{
    public class HiddenMap
    {
        public string Hash { get; set; }

        public HiddenMap() { }

        public HiddenMap(Map map)
        {
            Hash = map.Hash;
        }

        public override bool Equals(object? obj)
        {
            return Hash == (obj as HiddenMap)?.Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }
    }
}
