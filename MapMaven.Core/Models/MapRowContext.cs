using MapMaven.Models;

namespace MapMaven.Core.Models
{
    public class MapRowContext
    {
        public IEnumerable<Map> FilteredMaps { get; set; }
        public Map Map { get; set; }
        public bool Selectable { get; set; }
    }
}
