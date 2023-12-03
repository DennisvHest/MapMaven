using BeatSaverSharp.Models;
using MapMaven.Core.Models.Data.RankedMaps;

namespace MapMaven.Core.Models
{
    public class MapDifficulty
    {
        public double? Stars { get; set; }
        public string Difficulty { get; set; }
        public string? Label { get; set; }
        public double Njs { get; set; }
        public double Offset { get; set; }
        public int Notes { get; set; }
        public int Bombs { get; set; }
        public int Obstacles { get; set; }
        public double NotesPerSecond { get; set; }
        public int MaxScore { get; set; }

        public MapDifficulty() { }

        public MapDifficulty(BeatmapDifficulty beatmapDifficulty)
        {
            Difficulty = beatmapDifficulty.Difficulty.ToString();
            Njs = beatmapDifficulty.NJS;
            Offset = beatmapDifficulty.Offset;
            Notes = beatmapDifficulty.Notes;
            Bombs = beatmapDifficulty.Bombs;
            Obstacles = beatmapDifficulty.Obstacles;
            NotesPerSecond = beatmapDifficulty.NPS;
        }

        public MapDifficulty(RankedMapDifficultyInfo difficultyInfo)
        {
            Stars = difficultyInfo.Stars;
            Difficulty = difficultyInfo.Difficulty.ToString();
            Label = difficultyInfo.Label;
            Njs = difficultyInfo.Njs;
            Offset = difficultyInfo.Offset;
            Notes = difficultyInfo.Notes;
            Bombs = difficultyInfo.Bombs;
            Obstacles = difficultyInfo.Obstacles;
            NotesPerSecond = difficultyInfo.NotesPerSecond;
            MaxScore = difficultyInfo.MaxScore;
        }
    }
}
