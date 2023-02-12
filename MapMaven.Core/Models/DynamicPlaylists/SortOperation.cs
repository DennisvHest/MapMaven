using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class SortOperation
    {
        public string Field { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SortDirection Direction { get; set; }
    }
}
