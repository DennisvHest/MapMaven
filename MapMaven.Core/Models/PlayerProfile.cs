using System.Linq;

namespace MapMaven.Core.Models
{
    public class PlayerProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public string ProfilePictureUrl { get; set; }
        public int Rank { get; set; }
        public int CountryRank { get; set; }
        public double Pp { get; set; }
        public LeaderboardProvider LeaderboardProvider { get; set; }
        public IEnumerable<RankHistoryRecord> RankHistory { get; set; } = [];

        public PlayerProfile() {}

        public PlayerProfile(ApiClients.ScoreSaber.Player player)
        {
            Id = player.Id;
            Name = player.Name;
            CountryCode = player.Country;
            ProfilePictureUrl = player.ProfilePicture;
            Rank = Convert.ToInt32(player.Rank);
            CountryRank = Convert.ToInt32(player.CountryRank);
            Pp = player.Pp;
            LeaderboardProvider = LeaderboardProvider.ScoreSaber;

            var playerRankHistory = player.Histories
                ?.Split(',')
                .Select(int.Parse)
                .Cast<int?>()
                .ToArray() ?? [];

            RankHistory = [new RankHistoryRecord() { Date = DateOnly.FromDateTime(DateTime.Today), Rank = Rank }];

            RankHistory = RankHistory.Concat(Enumerable.Range(1, 49).Select(dateOffset => new RankHistoryRecord
            {
                Rank = playerRankHistory.ElementAtOrDefault(playerRankHistory.Length - dateOffset),
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-dateOffset))
            }));

            RankHistory = RankHistory.OrderBy(x => x.Date);
        }

        public PlayerProfile(ApiClients.BeatLeader.PlayerResponseFull playerProfile)
        {
            Id = playerProfile.Id;
            Name = playerProfile.Name;
            CountryCode = playerProfile.Country;
            ProfilePictureUrl = playerProfile.Avatar;
            Rank = Convert.ToInt32(playerProfile.Rank);
            CountryRank = Convert.ToInt32(playerProfile.CountryRank);
            Pp = playerProfile.Pp;
            LeaderboardProvider = LeaderboardProvider.BeatLeader;
            RankHistory = [];
        }
    }
}
