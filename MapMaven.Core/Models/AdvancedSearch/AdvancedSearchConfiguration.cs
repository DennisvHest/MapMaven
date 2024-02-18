using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.AdvancedSearch
{
    public class AdvancedSearchConfiguration
    {
        [ValidateComplexType]
        public List<FilterOperation> FilterOperations { get; set; } = new();

        [ValidateComplexType]
        public List<SortOperation> SortOperations { get; set; } = new();
    }
}
