using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class FilterOperation
    {
        public string Field { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FilterOperator Operator { get; set; }
        public string Value { get; set; }
    }
}
