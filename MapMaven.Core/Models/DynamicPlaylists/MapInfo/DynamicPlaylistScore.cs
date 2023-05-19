namespace MapMaven.Core.Models.DynamicPlaylists.MapInfo
{
    public class DynamicPlaylistScore
    {
        public double Rank { get; set; }
        public double BaseScore { get; set; }
        public double ModifiedScore { get; set; }
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
    }
}
