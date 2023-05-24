using MapMaven.Core.Utilities.Scoresaber;
using System.ComponentModel;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistScoreEstimate
    {
        [DisplayName("Estimated accuracy")]
        public double Accuracy { get; set; }

        [DisplayName("PP Value")]
        public double Pp { get; set; }

        [DisplayName("Estimated PP increase")]
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
