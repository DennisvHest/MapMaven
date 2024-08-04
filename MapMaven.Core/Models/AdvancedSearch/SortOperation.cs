using MapMaven.Core.Models.LivePlaylists;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.AdvancedSearch
{
    public class SortOperation
    {
        [Required(ErrorMessage = "Field is required.")]
        public string Field { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SortDirection Direction { get; set; }
    }
}
