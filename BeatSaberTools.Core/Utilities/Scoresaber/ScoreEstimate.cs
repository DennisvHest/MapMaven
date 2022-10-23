namespace BeatSaberTools.Core.Utilities.Scoresaber
{
    public class ScoreEstimate
    {
        public string MapId { get; set; }
        public decimal Accuracy { get; set; }
        public decimal PP { get; set; }
        public decimal TotalPP { get; set; }
        public decimal PPIncrease { get; set; }
        public string? Difficulty { get; set; }
        public decimal Stars { get; set; }
    }
}
