using MapMaven.Models;

namespace MapMaven.Core.Models
{
    public class MapFilter
    {
        public MapFilterType? Type { get; set; }
        public string Name { get; set; }
        public Func<Map, bool> Filter { get; set; }
        public bool Visible { get; set; } = true;
    }

    public enum MapFilterType
    {
        Played
    }
}
