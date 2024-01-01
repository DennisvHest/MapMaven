
namespace MapMaven.Core.Utilities.Scoresaber
{
    public static class ScoreSaberExtensions
    {
        public static double AccuracyWithMods(this ApiClients.ScoreSaber.PlayerScore score)
        {
            if (score.Leaderboard.MaxScore == 0)
                return 0;

            return score.Score.ModifiedScore / score.Leaderboard.MaxScore * 100;
        }

        public static double Accuracy(this ApiClients.ScoreSaber.PlayerScore score)
        {
            if (score.Leaderboard.MaxScore == 0)
                return 0;

            return score.Score.BaseScore / score.Leaderboard.MaxScore * 100;
        }

        public static double AccuracyWithMods(this ApiClients.BeatLeader.ScoreResponseWithMyScore score)
        {
            var maxScore = score.Leaderboard?.Difficulty?.MaxScore;

            if (!maxScore.HasValue)
                return 0;

            return score.ModifiedScore / (double)maxScore.Value * 100;
        }

        public static double Accuracy(this ApiClients.BeatLeader.ScoreResponseWithMyScore score)
        {
            var maxScore = score.Leaderboard?.Difficulty?.MaxScore;

            if (!maxScore.HasValue)
                return 0;

            return score.BaseScore / (double)maxScore.Value * 100;
        }
    }
}
