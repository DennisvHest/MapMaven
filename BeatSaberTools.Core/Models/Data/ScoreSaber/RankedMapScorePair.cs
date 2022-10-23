using BeatSaberTools.Core.ApiClients;

namespace BeatSaberTools.Core.Models.Data.ScoreSaber
{
    public class RankedMapScorePair
    {
        public RankedMap Map { get; set; }
        public PlayerScore PlayerScore { get; set; }
    }
}
