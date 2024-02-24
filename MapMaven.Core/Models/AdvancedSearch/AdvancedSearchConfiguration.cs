using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.AdvancedSearch
{
    public class AdvancedSearchConfiguration
    {
        [ValidateComplexType]
        public List<MapFilterOperationPair> FilterOperations { get; set; } = new();

        [ValidateComplexType]
        public List<SortOperation> SortOperations { get; set; } = new();
    }
}
