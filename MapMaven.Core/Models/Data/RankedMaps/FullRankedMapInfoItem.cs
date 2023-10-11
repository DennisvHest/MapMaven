using MapMaven.Core.ApiClients.BeatSaver;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public abstract class FullRankedMapInfoItem
    {
        public string SongHash { get; set; }
        public MapDetail MapDetail { get; set; }
    }
}
