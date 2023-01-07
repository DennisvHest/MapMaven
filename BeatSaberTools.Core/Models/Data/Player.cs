namespace BeatSaberTools.Core.Models.Data
{
    public class Player
    {
        public string Id { get; set; }

        public virtual ICollection<HiddenMap> HiddenMaps { get; set; }
    }
}
