using Microsoft.EntityFrameworkCore;

namespace MapMaven.DataGatherers.BeatLeader.Data
{
    public class BeatLeaderScoresContext : DbContext
    {
        public DbSet<PlayerResponseWithStats> Players { get; set; }

        public BeatLeaderScoresContext(DbContextOptions<BeatLeaderScoresContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var players = modelBuilder.Entity<PlayerResponseWithStats>().ToTable("Players");

            players.Property(x => x.Id).ValueGeneratedNever();

            players.Ignore(p => p.Socials);
            players.Ignore(p => p.ContextExtensions);
            players.Ignore(p => p.PatreonFeatures);
            players.Ignore(p => p.ProfileSettings);
            players.Ignore(p => p.Clans);
            players.Ignore(p => p.EventsParticipating);

            var playerScoreStats = modelBuilder.Entity<PlayerScoreStats>().ToTable("PlayerScoreStats");

            playerScoreStats.Property(x => x.Id).ValueGeneratedNever();
        }
    }
}
