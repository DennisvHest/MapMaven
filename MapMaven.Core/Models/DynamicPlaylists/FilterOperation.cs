using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class FilterOperation
    {
        [Required(ErrorMessage = "Field is required.")]
        public string Field { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FilterOperator Operator { get; set; }
        [Required(ErrorMessage = "Value is required.")]
        public string Value { get; set; }
    }
}
