namespace MapMaven.Core.Utilities.BeatSaver
{
    public static class MapTag
    {
        private static readonly string[] DisciplineTags = [ "accuracy", "balanced", "challenge", "dance", "fitness", "speed", "tech" ];

        public static bool IsDisciplineTag(string tag) => DisciplineTags.Contains(tag);
    }
}
