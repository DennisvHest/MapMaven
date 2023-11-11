using System.ComponentModel;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistScore
    {
        public double Rank { get; set; }

        [DisplayName("Base score")]
        public double BaseScore { get; set; }

        [DisplayName("Modified score")]
        public double ModifiedScore { get; set; }
        public double Accuracy { get; set; }

        [DisplayName("Score PP")]
        public double Pp { get; set; }
        public double Weight { get; set; }
        public double Multiplier { get; set; }

        [DisplayName("Bad cuts")]
        public double BadCuts { get; set; }

        [DisplayName("Missed notes")]
        public double MissedNotes { get; set; }

        [DisplayName("Max combo")]
        public double MaxCombo { get; set; }

        [DisplayName("Full combo")]
        public bool FullCombo { get; set; }

        [DisplayName("Has replay")]
        public bool HasReplay { get; set; }

        [DisplayName("Score: Time set")]
        public DateTime TimeSet { get; set; }

        public DynamicPlaylistScore() { }

        public DynamicPlaylistScore(PlayerScore score)
        {
            Rank = score.Score.Rank;
            BaseScore = score.Score.BaseScore;
            ModifiedScore = score.Score.ModifiedScore;
            Accuracy = score.Score.Accuracy;
            Pp = score.Score.Pp;
            Weight = score.Score.Weight;
            Multiplier = 1; // TODO: remove if safe (BeatLeader does not supply this field)
            BadCuts = score.Score.BadCuts;
            MissedNotes = score.Score.MissedNotes;
            MaxCombo = score.Score.MaxCombo;
            FullCombo = score.Score.FullCombo;
            HasReplay = score.Score.HasReplay;
            TimeSet = score.Score.TimeSet.LocalDateTime;
        }
    }
}
