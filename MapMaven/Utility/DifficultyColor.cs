using Colors = MudBlazor.Colors;

namespace BeatSaberTools.Utility
{
    public static class DifficultyColor
    {
        public static string Get(string difficulty)
        {
            return difficulty switch
            {
                "ExpertPlus" => Colors.Purple.Darken3,
                "Expert" => Colors.Red.Darken3,
                "Hard" => Colors.Orange.Darken3,
                "Normal" => Colors.Blue.Darken3,
                "Easy" => Colors.Green.Darken3,
                _ => Colors.Shades.Black
            };
        }
    }
}
