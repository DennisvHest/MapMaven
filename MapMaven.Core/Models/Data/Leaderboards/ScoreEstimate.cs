namespace MapMaven.Core.Models.Data.Leaderboards
{
    public class ScoreEstimate
    {
        public string MapHash { get; set; }
        public double Accuracy { get; set; }
        public double Pp { get; set; }
        public double TotalPP { get; set; }
        public double PPIncrease { get; set; }
        public string? Difficulty { get; set; }
        public double Stars { get; set; }
    }
}
