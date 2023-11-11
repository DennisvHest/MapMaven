﻿using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Models
{
    public class Score
    {
        public double Rank { get; set; }
        public double BaseScore { get; set; }
        public double ModifiedScore { get; set; }
        public double Accuracy { get; set; }
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
            Rank = playerScore.Score.Rank;
            BaseScore = playerScore.Score.BaseScore;
            ModifiedScore = playerScore.Score.ModifiedScore;
            Accuracy = playerScore.Accuracy();
            Pp = playerScore.Score.Pp;
            Weight = playerScore.Score.Weight;
            BadCuts = playerScore.Score.BadCuts;
            MissedNotes = playerScore.Score.MissedNotes;
            MaxCombo = playerScore.Score.MaxCombo;
            FullCombo = playerScore.Score.FullCombo;
            HasReplay = playerScore.Score.HasReplay;
            TimeSet = playerScore.Score.TimeSet;
        }

        public Score(ApiClients.BeatLeader.Score score)
        {
            Rank = score.Rank;
            BaseScore = score.BaseScore;
            ModifiedScore = score.ModifiedScore;
            Accuracy = score.Accuracy * 100;
            Pp = score.Pp;
            Weight = score.Weight;
            BadCuts = score.BadCuts;
            MissedNotes = score.MissedNotes;
            MaxCombo = score.MaxCombo;
            FullCombo = score.FullCombo;
            HasReplay = true;
            TimeSet = DateTimeOffset.FromUnixTimeSeconds(score.Timepost);
        }

        public Score(ApiClients.BeatLeader.ScoreResponseWithMyScore score)
        {
            Rank = score.Rank;
            BaseScore = score.BaseScore;
            ModifiedScore = score.ModifiedScore;
            Accuracy = score.Accuracy * 100;
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
