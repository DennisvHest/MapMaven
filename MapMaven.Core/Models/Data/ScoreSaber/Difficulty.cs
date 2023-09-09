namespace MapMaven.Core.ApiClients.ScoreSaber
{
    public partial class Difficulty
    {
        public string DifficultyName => DifficultyRaw.Split('_')[1];
    }
}
