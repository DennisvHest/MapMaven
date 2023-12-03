using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data.Leaderboards.BeatLeader;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class RankedMapDifficultyInfo
    {
        public double Stars { get; set; }
        public string Difficulty { get; set; }
        public string? Label { get; set; }
        public double Njs { get; set; }
        public double Offset { get; set; }
        public int Notes { get; set; }
        public int Bombs { get; set; }
        public int Obstacles { get; set; }
        public double NotesPerSecond { get; set; }
        public int MaxScore { get; set; }

        public BeatLeaderRating? BeatLeaderRating { get; set; }

        public RankedMapDifficultyInfo() { }

        public RankedMapDifficultyInfo(LeaderboardInfo leaderboard, ApiClients.BeatSaver.MapDifficulty difficulty)
        {
            Stars = leaderboard.Stars;
            Difficulty = leaderboard.Difficulty.DifficultyName;

            SetBeatSaverMapDiffultyProperties(difficulty);
        }

        public RankedMapDifficultyInfo(LeaderboardInfoResponse leaderboard, ApiClients.BeatSaver.MapDifficulty difficulty)
        {
            var stars = (double)leaderboard.Difficulty.Stars;

            Stars = stars;
            BeatLeaderRating = new()
            {
                PassRating = leaderboard.Difficulty.PassRating.Value,
                AccRating = leaderboard.Difficulty.AccRating.Value,
                TechRating = leaderboard.Difficulty.TechRating.Value
            };
            Difficulty = leaderboard.Difficulty.DifficultyName;

            SetBeatSaverMapDiffultyProperties(difficulty);
        }

        protected void SetBeatSaverMapDiffultyProperties(ApiClients.BeatSaver.MapDifficulty difficulty)
        {
            Label = difficulty.Label;
            Njs = difficulty.Njs ?? 0;
            Offset = difficulty.Offset ?? 0;
            Notes = difficulty.Notes ?? 0;
            Bombs = difficulty.Bombs ?? 0;
            Obstacles = difficulty.Obstacles ?? 0;
            NotesPerSecond = difficulty.Nps ?? 0;
            MaxScore = difficulty.MaxScore ?? 0;
        }
    }
}
