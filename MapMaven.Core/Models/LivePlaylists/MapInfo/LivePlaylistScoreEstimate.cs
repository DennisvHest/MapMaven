using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Utilities.LivePlaylists;
using System.ComponentModel;

namespace MapMaven.Core.Models.LivePlaylists.MapInfo
{
    public class LivePlaylistScoreEstimate
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

        public LivePlaylistScoreEstimate() { }

        public LivePlaylistScoreEstimate(ScoreEstimate scoreEstimate)
        {
            Accuracy = scoreEstimate.Accuracy;
            Pp = scoreEstimate.Pp;
            PPIncrease = scoreEstimate.PPIncrease;
        }
    }
}
