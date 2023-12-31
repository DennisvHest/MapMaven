using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Models
{
    public class Score
    {
        public string Id { get; set; }
        public double Rank { get; set; }
        public double BaseScore { get; set; }
        public double ModifiedScore { get; set; }
        public double Accuracy { get; set; }
        public double AccuracyWithMods { get; set; }
        public IEnumerable<string> Modifiers { get; set; } = Enumerable.Empty<string>();
        public double Pp { get; set; }
        public double Weight { get; set; }
        public double BadCuts { get; set; }
        public double MissedNotes { get; set; }
        public double MaxCombo { get; set; }
        public bool FullCombo { get; set; }
        public bool HasReplay { get; set; }
        public DateTimeOffset TimeSet { get; set; }

        public Score() { }

        public Score(ApiClients.ScoreSaber.PlayerScore playerScore)
        {
            Id = playerScore.Score.Id.ToString();
            Rank = playerScore.Score.Rank;
            BaseScore = playerScore.Score.BaseScore;
            ModifiedScore = playerScore.Score.ModifiedScore;
            Accuracy = playerScore.Accuracy();
            AccuracyWithMods = playerScore.AccuracyWithMods();
            Modifiers = playerScore.Score.Modifiers?.Split(',') ?? Enumerable.Empty<string>();
            Pp = playerScore.Score.Pp;
            Weight = playerScore.Score.Weight;
            BadCuts = playerScore.Score.BadCuts;
            MissedNotes = playerScore.Score.MissedNotes;
            MaxCombo = playerScore.Score.MaxCombo;
            FullCombo = playerScore.Score.FullCombo;
            HasReplay = playerScore.Score.HasReplay;
            TimeSet = playerScore.Score.TimeSet;
        }

        public Score(ApiClients.BeatLeader.ScoreResponseWithMyScore score)
        {
            Id = score.Id.ToString();
            Rank = score.Rank;
            BaseScore = score.BaseScore;
            ModifiedScore = score.ModifiedScore;
            Accuracy = score.Accuracy();
            AccuracyWithMods = score.AccuracyWithMods();
            Modifiers = score.Modifiers?.Split(',') ?? Enumerable.Empty<string>();
            Pp = score.Pp;
            Weight = score.Weight;
            BadCuts = score.BadCuts;
            MissedNotes = score.MissedNotes;
            MaxCombo = score.MaxCombo;
            FullCombo = score.FullCombo;
            HasReplay = true;
            TimeSet = DateTimeOffset.FromUnixTimeSeconds(score.Timepost);
        }
    }
}
