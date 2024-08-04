using MapMaven.Core.Models.LivePlaylists;

namespace MapMaven.Utilities.LivePlaylists
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
