using BeatSaberTools.Core.ApiClients;

namespace BeatSaberTools.Core.Utilities.Scoresaber
{
    public static class ScoreSaberExtensions
    {
        public static decimal AccuracyWithMods(this PlayerScore score) => Convert.ToDecimal(score.Score.ModifiedScore / score.Leaderboard.MaxScore * 100);

        public static decimal Accuracy(this PlayerScore score) => Convert.ToDecimal(score.Score.BaseScore / score.Leaderboard.MaxScore * 100);
    }
}
