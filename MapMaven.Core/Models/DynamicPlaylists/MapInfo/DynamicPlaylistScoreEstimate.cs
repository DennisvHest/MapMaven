using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistScoreEstimate
    {
        public double Accuracy { get; set; }
        public double Pp { get; set; }
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
