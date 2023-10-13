using MapMaven.Core.ApiClients.BeatLeader;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class BeatLeaderRankedMapDifficultyInfo : RankedMapDifficultyInfo
    {
        public float PassRating { get; set; }
        public float AccRating { get; set; }
        public float TechRating { get; set; }

        public BeatLeaderRankedMapDifficultyInfo() { }

        public BeatLeaderRankedMapDifficultyInfo(LeaderboardInfoResponse leaderboard, ApiClients.BeatSaver.MapDifficulty difficulty)
        {
            var stars = (double)leaderboard.Difficulty.Stars;

            Stars = stars;
            MaxPP = stars * 34; // TODO: Replace this with BeatLeader specific PP calculation properties
            PassRating = leaderboard.Difficulty.PassRating.Value;
            AccRating = leaderboard.Difficulty.AccRating.Value;
            TechRating = leaderboard.Difficulty.TechRating.Value;
            Difficulty = leaderboard.Difficulty.DifficultyName;

            SetBeatSaverMapDiffultyProperties(difficulty);
        }
    }
}
