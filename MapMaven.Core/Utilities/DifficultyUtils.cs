namespace MapMaven.Core.Utilities
{
    public static class DifficultyUtils
    {
        public static int GetOrder(string? difficulty)
        {
            return difficulty switch
            {
                "ExpertPlus" => 5,
                "Expert" => 4,
                "Hard" => 3,
                "Normal" => 2,
                "Easy" => 1,
                _ => 0
            };
        }

        public static IEnumerable<string> Difficulties => [
            "Easy",
            "Normal",
            "Hard",
            "Expert",
            "ExpertPlus"
        ];
    }
}
