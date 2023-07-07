using MapMaven.Core.Utilities.Scoresaber;
using MapMaven.Utilities.DynamicPlaylists;
using System.ComponentModel;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistScoreEstimate
    {
        [DisplayName("Estimated accuracy")]
        [ApplicableForMapPool(MapPool.Improvement)]
        public double Accuracy { get; set; }

        [DisplayName("Estimated PP value")]
        [ApplicableForMapPool(MapPool.Improvement)]
        public double Pp { get; set; }

        [DisplayName("Estimated total PP increase")]
        [ApplicableForMapPool(MapPool.Improvement)]
        public double PPIncrease { get; set; }

        public DynamicPlaylistScoreEstimate() { }

        public DynamicPlaylistScoreEstimate(ScoreEstimate scoreEstimate)
        {
            Accuracy = scoreEstimate.Accuracy;
            Pp = scoreEstimate.Pp;
            PPIncrease = scoreEstimate.PPIncrease;
        }
    }
}
