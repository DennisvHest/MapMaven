namespace BeatSaberTools.Core.Models
{
    public class HiddenMapConfigurationItem
    {
        public string PlayerId { get; set; }
        public HashSet<HiddenMap> Maps { get; set; } = new();
    }
}
