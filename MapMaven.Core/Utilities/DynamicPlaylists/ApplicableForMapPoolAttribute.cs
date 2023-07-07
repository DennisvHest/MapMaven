using MapMaven.Core.Models.DynamicPlaylists;

namespace MapMaven.Utilities.DynamicPlaylists
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ApplicableForMapPoolAttribute : Attribute
    {
        public MapPool[] MapPools { get; }

        public ApplicableForMapPoolAttribute(params MapPool[] mapPools)
        {
            MapPools = mapPools;
        }
    }
}
