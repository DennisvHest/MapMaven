using Microsoft.EntityFrameworkCore;

namespace MapMaven.DataGatherers.BeatLeader
{
    public partial class PlayerResponseWithStats
    {
        public virtual ICollection<ScoreResponseWithMyScore> Scores { get; set; }
    }

    public partial class ScoreResponseWithMyScore
    {
        public virtual PlayerResponseWithStats PlayerEntity { get; set; }
    }

    public partial class LeaderboardResponse
    {
        public string SongId { get; set; }
    }
}

namespace MapMaven.DataGatherers.BeatLeader.Data
{
    public class BeatLeaderScoresContext : DbContext
    {
        public DbSet<PlayerResponseWithStats> Players { get; set; }
        public DbSet<ScoreResponseWithMyScore> Scores { get; set; }
        public DbSet<LeaderboardResponse> Leaderboards { get; set; }
        public DbSet<Song> Songs { get; set; }

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

            players.OwnsOne(p => p.ScoreStats);

            players.HasMany(p => p.Scores)
                .WithOne(s => s.PlayerEntity)
                .HasForeignKey(x => x.PlayerId);

            var scores = modelBuilder.Entity<ScoreResponseWithMyScore>().ToTable("Scores");

            scores.Property(x => x.Id).ValueGeneratedNever();

            scores.Ignore(s => s.Player);
            scores.Ignore(s => s.ScoreImprovement);
            scores.Ignore(s => s.RankVoting);
            scores.Ignore(s => s.Metadata);
            scores.Ignore(s => s.Offsets);
            scores.Ignore(s => s.MyScore);
            scores.Ignore(s => s.ContextExtensions);

            scores.HasOne(s => s.Leaderboard)
                .WithMany()
                .HasForeignKey(x => x.LeaderboardId);

            var leaderboards = modelBuilder.Entity<LeaderboardResponse>().ToTable("Leaderboards");

            leaderboards.Property(x => x.Id).ValueGeneratedNever();

            leaderboards.Ignore(l => l.Scores);
            leaderboards.Ignore(l => l.Changes);
            leaderboards.Ignore(l => l.Qualification);
            leaderboards.Ignore(l => l.Reweight);
            leaderboards.Ignore(l => l.LeaderboardGroup);
            leaderboards.Ignore(l => l.Clan);

            leaderboards.OwnsOne(l => l.Difficulty, difficulty =>
            {
                difficulty.OwnsOne(d => d.ModifierValues);
                difficulty.OwnsOne(d => d.ModifiersRating);
            });

            leaderboards
                .HasOne(l => l.Song)
                .WithMany()
                .HasForeignKey(l => l.SongId);

            var songs = modelBuilder.Entity<Song>().ToTable("Songs");

            songs.Property(x => x.Id).ValueGeneratedNever();

            songs.Ignore(s => s.Difficulties);
        }
    }
}
