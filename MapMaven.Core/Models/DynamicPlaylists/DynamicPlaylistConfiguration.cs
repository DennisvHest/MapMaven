using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class DynamicPlaylistConfiguration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MapPool MapPool { get; set; } = MapPool.Standard;
        public IEnumerable<FilterOperation> FilterOperations { get; set; } = Enumerable.Empty<FilterOperation>();
        public IEnumerable<SortOperation> SortOperations { get; set; } = Enumerable.Empty<SortOperation>();
        public int MapCount { get; set; }
    }
}
