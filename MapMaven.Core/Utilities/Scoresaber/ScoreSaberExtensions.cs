using MapMaven.Core.ApiClients.ScoreSaber;

namespace MapMaven.Core.Utilities.Scoresaber
{
    public static class ScoreSaberExtensions
    {
        public static double AccuracyWithMods(this PlayerScore score)
        {
            if (score.Leaderboard.MaxScore == 0)
                return 0;

            return score.Score.ModifiedScore / score.Leaderboard.MaxScore * 100;
        }

        public static double Accuracy(this PlayerScore score)
        {
            if (score.Leaderboard.MaxScore == 0)
                return 0;

            return score.Score.BaseScore / score.Leaderboard.MaxScore * 100;
        }
    }
}
