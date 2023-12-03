namespace MapMaven.Core.Models.Data.Leaderboards.BeatLeader
{
    public class BeatLeaderLeaderboardData
    {
        public PPCurveItem[] AccCurve { get; set; }

        public double PpDecay { get; set; }
        public double PassMultiplier { get; set; }
        public double PassExponential { get; set; }
        public double PassShift { get; set; }
        public double AccMultiplier { get; set; }
        public double TechMultiplier { get; set; }
        public double TechExponentialMultiplier { get; set; }
        public double InflateMultiplier { get; set; }
        public double InflateExponential { get; set; }
    }
}
