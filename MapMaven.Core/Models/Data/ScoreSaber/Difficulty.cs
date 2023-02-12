namespace MapMaven.Core.ApiClients
{
    public partial class Difficulty
    {
        public string DifficultyName => DifficultyRaw.Split('_')[1];
    }
}
