using BeatSaberTools.Models;

namespace BeatSaberTools.Core.Models
{
    public class MapFilter
    {
        public string Name { get; set; }
        public Func<Map, bool> Filter { get; set; }
    }
}
