using BeatSaberTools.Core.Models.Data;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace BeatSaberTools.Infrastructure.Data
{
    public class BSToolsContext : DbContext, IDataStore
    {
        public DbSet<MapInfo> MapInfos { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<HiddenMap> HiddenMaps { get; set; }

        public static string DbPath => Path.Join(BeatSaverFileServiceBase.AppDataLocation, "BSTools.db");

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!Directory.Exists(BeatSaverFileServiceBase.AppDataLocation))
                Directory.CreateDirectory(BeatSaverFileServiceBase.AppDataLocation);

            options.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var mapInfo = modelBuilder.Entity<MapInfo>();

            mapInfo.Ignore(i => i.PreviewStartTime);
            mapInfo.Ignore(i => i.PreviewDuration);

            var player = modelBuilder.Entity<Player>();

            player
                .HasMany(p => p.HiddenMaps)
                .WithOne(m => m.Player)
                .HasForeignKey(m => m.PlayerId);

            var hiddenMap = modelBuilder.Entity<HiddenMap>();

            hiddenMap
                .Property(m => m.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
