using BeatSaberTools.Core.Services;
using BeatSaberTools.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace BeatSaberTools.Infrastructure.Data
{
    public class BSToolsContext : DbContext
    {
        public DbSet<MapInfo> MapInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!Directory.Exists(BeatSaverFileServiceBase.AppDataLocation))
                Directory.CreateDirectory(BeatSaverFileServiceBase.AppDataLocation);

            var dbPath = Path.Join(BeatSaverFileServiceBase.AppDataLocation, "BSTools.db");

            options.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var mapInfo = modelBuilder.Entity<MapInfo>();

            mapInfo.Ignore(i => i.PreviewStartTime);
            mapInfo.Ignore(i => i.PreviewDuration);
        }
    }
}
