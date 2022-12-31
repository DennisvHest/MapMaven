using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace BeatSaberTools.Core.Models.DynamicPlaylists
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
