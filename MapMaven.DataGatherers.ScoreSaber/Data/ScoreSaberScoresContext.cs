namespace MapMaven.DataGatherers.ScoreSaber
{
    public partial class Player
    {
        public virtual ICollection<PlayerScore> PlayerScores { get; set; }
    }

    public partial class PlayerScore
    {
        public int Id { get; set; }
        public string PlayerId { get; set; }
    }

    public partial class Difficulty
    {
        public int Id { get; set; }
    }
}

namespace MapMaven.DataGatherers.ScoreSaber.Data
{
    using Microsoft.EntityFrameworkCore;

    public class ScoreSaberScoresContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerScore> PlayerScores { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<LeaderboardInfo> Leaderboards { get; set; }

        public ScoreSaberScoresContext(DbContextOptions<ScoreSaberScoresContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=BSScores;Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Badge>();
            modelBuilder.Ignore<LeaderboardPlayer>();

            modelBuilder.Entity<Player>(x =>
            {
                x.OwnsOne(x => x.ScoreStats, s =>
                {
                    s.Property(x => x.TotalScore).HasColumnName("TotalScore");
                    s.Property(x => x.TotalRankedScore).HasColumnName("TotalRankedScore");
                    s.Property(x => x.AverageRankedAccuracy).HasColumnName("AverageRankedAccuracy");
                    s.Property(x => x.TotalPlayCount).HasColumnName("TotalPlayCount");
                    s.Property(x => x.RankedPlayCount).HasColumnName("RankedPlayCount");
                    s.Property(x => x.ReplaysWatched).HasColumnName("ReplaysWatched");
                });
            });

            var player = modelBuilder.Entity<Player>();

            player.Property(x => x.Role).IsRequired(false);
            player.HasMany(p => p.PlayerScores).WithOne().HasForeignKey(x => x.PlayerId);

            var leaderboardInfo = modelBuilder.Entity<LeaderboardInfo>();

            leaderboardInfo.Ignore(l => l.PlayerScore);

            modelBuilder.Entity<PlayerScore>().HasOne(x => x.Score).WithMany().OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PlayerScore>().HasOne(x => x.Leaderboard).WithMany().OnDelete(DeleteBehavior.NoAction);
        }
    }
}
