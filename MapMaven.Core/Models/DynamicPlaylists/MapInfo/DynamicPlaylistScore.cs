using MapMaven.Core.ApiClients;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistScore
    {
        public double Rank { get; set; }
        public double BaseScore { get; set; }
        public double ModifiedScore { get; set; }
        public double Accuracy { get; set; }
        public double Pp { get; set; }
        public double Weight { get; set; }
        public double Multiplier { get; set; }
        public double BadCuts { get; set; }
        public double MissedNotes { get; set; }
        public double MaxCombo { get; set; }
        public bool FullCombo { get; set; }
        public double Hmd { get; set; }
        public bool HasReplay { get; set; }
        public DateTimeOffset TimeSet { get; set; }

        public DynamicPlaylistScore() { }

        public DynamicPlaylistScore(PlayerScore score)
        {
            Rank = score.Score.Rank;
            BaseScore = score.Score.BaseScore;
            ModifiedScore = score.Score.ModifiedScore;
            Accuracy = score.AccuracyWithMods();
            Pp = score.Score.Pp;
            Weight = score.Score.Weight;
            Multiplier = score.Score.Multiplier;
            BadCuts = score.Score.BadCuts;
            MissedNotes = score.Score.MissedNotes;
            MaxCombo = score.Score.MaxCombo;
            FullCombo = score.Score.FullCombo;
            Hmd = score.Score.Hmd;
            HasReplay = score.Score.HasReplay;
            TimeSet = score.Score.TimeSet;
        }
    }
}
