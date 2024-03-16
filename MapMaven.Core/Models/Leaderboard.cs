namespace MapMaven.Core.Models
{
    public class Leaderboard
    {
        public string SongHash { get; set; }
        public string SongName { get; set; }
        public string SongAuthorName { get; set; }
        public string LevelAuthorName { get; set; }
        public string Difficulty { get; set; }
        public double? Stars { get; set; }

        public bool Ranked => Stars > 0;

        public Leaderboard() { }

        public Leaderboard(ApiClients.ScoreSaber.LeaderboardInfo leaderboard)
        {
            SongHash = leaderboard.SongHash;
            SongName = leaderboard.SongName;
            SongAuthorName = leaderboard.SongAuthorName;
            LevelAuthorName = leaderboard.LevelAuthorName;
            Difficulty = leaderboard.Difficulty?.DifficultyName;
            Stars = leaderboard.Stars;
        }

        public Leaderboard(ApiClients.BeatLeader.Leaderboard leaderboard)
        {
            SongHash = leaderboard.Song.Hash.ToUpper();
            SongName = leaderboard.Song.Name;
            SongAuthorName = leaderboard.Song.Author;
            LevelAuthorName = leaderboard.Song.Mapper;
            Difficulty = leaderboard.Difficulty.DifficultyName;
            Stars = leaderboard.Difficulty.Stars;
        }

        public Leaderboard(ApiClients.BeatLeader.LeaderboardResponse leaderboard)
        {
            SongHash = leaderboard.Song.Hash.ToUpper();
            SongName = leaderboard.Song.Name;
            SongAuthorName = leaderboard.Song.Author;
            LevelAuthorName = leaderboard.Song.Mapper;
            Difficulty = leaderboard.Difficulty.DifficultyName;
            Stars = leaderboard.Difficulty.Stars;
        }
    }
}
