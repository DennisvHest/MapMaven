namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class FullRankedMapInfo<TFullRankedMapInfoItem> where TFullRankedMapInfoItem : FullRankedMapInfoItem
    {
        public IEnumerable<TFullRankedMapInfoItem> RankedMaps { get; set; } = Array.Empty<TFullRankedMapInfoItem>();
    }
}
