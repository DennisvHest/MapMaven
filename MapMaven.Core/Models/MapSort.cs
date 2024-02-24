using MapMaven.Models;

namespace MapMaven.Core.Models
{
    public class MapSort
    {
        public string Name { get; set; }
        public Func<IEnumerable<Map>, IEnumerable<Map>> Sort { get; set; }
    }
}
