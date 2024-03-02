using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.AdvancedSearch
{
    public class MapFilterOperationPair
    {
        [ValidateComplexType]
        public FilterOperation FilterOperation { get; set; } = new();
        public MapFilter? MapFilter { get; set; }
    }
}
