namespace MapMaven.Core.Models.AdvancedSearch
{
    public class MapFilterOperationPair
    {
        public FilterOperation FilterOperation { get; set; } = new();
        public MapFilter? MapFilter { get; set; }
    }
}
