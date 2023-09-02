using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Models.Data.RankedMaps
{
    public class RankedMapDifficultyInfo
    {
        public double Stars { get; set; }
        public double MaxPP { get; set; }
        public string Difficulty { get; set; }
        public string? Label { get; set; }
        public double Njs { get; set; }
        public double Offset { get; set; }
        public int Notes { get; set; }
        public int Bombs { get; set; }
        public int Obstacles { get; set; }
        public double NotesPerSecond { get; set; }
        public int MaxScore { get; set; }

        public RankedMapDifficultyInfo() { }

        public RankedMapDifficultyInfo(LeaderboardInfo leaderboard, ApiClients.BeatSaver.MapDifficulty difficulty)
        {
            Stars = leaderboard.Stars;
            MaxPP = leaderboard.Stars * Scoresaber.PPPerStar;
            Difficulty = leaderboard.Difficulty.DifficultyName;
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
