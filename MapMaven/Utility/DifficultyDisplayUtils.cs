using Colors = MudBlazor.Colors;

namespace MapMaven.Utility
{
    public static class DifficultyDisplayUtils
    {
        public static string GetColor(string difficulty)
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

        public static string GetShortName(string difficulty)
        {
            return difficulty switch
            {
                "ExpertPlus" => "EX+",
                "Expert" => "EX",
                "Hard" => "HD",
                "Normal" => "NM",
                "Easy" => "EZ",
                _ => "??"
            };
        }

        public static string GetDisplayName(string difficulty)
        {
            return difficulty switch
            {
                "ExpertPlus" => "Expert+",
                _ => difficulty
            };
        }
    }
}
