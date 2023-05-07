using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class DynamicPlaylistConfiguration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MapPool MapPool { get; set; } = MapPool.Standard;
        public List<FilterOperation> FilterOperations { get; set; } = new();
        public List<SortOperation> SortOperations { get; set; } = new();
        public int MapCount { get; set; }
    }
}
