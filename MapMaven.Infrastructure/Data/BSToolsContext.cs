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
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }

        public static string DbPath => Path.Join(BeatSaverFileService.AppDataLocation, "BSTools.db");

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!Directory.Exists(BeatSaverFileService.AppDataLocation))
                Directory.CreateDirectory(BeatSaverFileService.AppDataLocation);

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

            var applicationSetting = modelBuilder.Entity<ApplicationSetting>();

            applicationSetting
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            applicationSetting
                .Property(s => s.Key)
                .HasMaxLength(50);
        }
    }
}
