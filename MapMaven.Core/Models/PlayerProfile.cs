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
        }
    }
}
