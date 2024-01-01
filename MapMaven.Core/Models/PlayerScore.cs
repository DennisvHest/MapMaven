namespace MapMaven.Core.Models
{
    public class PlayerScore
    {
        public Score Score { get; set; }
        public Leaderboard Leaderboard { get; set; }

        public PlayerScore() { }

        public PlayerScore(ApiClients.ScoreSaber.PlayerScore playerScore)
        {
            Score = new Score(playerScore);
            Leaderboard = new Leaderboard(playerScore.Leaderboard);
        }

        public PlayerScore(ApiClients.BeatLeader.ScoreResponseWithMyScore score)
        {
            Score = new Score(score);
            Leaderboard = new Leaderboard(score.Leaderboard);
        }
    }
}
