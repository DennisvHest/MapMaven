using MapMaven.Core.ApiClients;

namespace MapMaven.Core.Utilities.Scoresaber
{
    public static class ScoreSaberExtensions
    {
        public static decimal AccuracyWithMods(this PlayerScore score)
        {
            if (score.Leaderboard.MaxScore == 0)
                return 0;

            return Convert.ToDecimal(score.Score.ModifiedScore / score.Leaderboard.MaxScore * 100);
        }

        public static decimal Accuracy(this PlayerScore score)
        {
            if (score.Leaderboard.MaxScore == 0)
                return 0;

            return Convert.ToDecimal(score.Score.BaseScore / score.Leaderboard.MaxScore * 100);
        }
    }
}
