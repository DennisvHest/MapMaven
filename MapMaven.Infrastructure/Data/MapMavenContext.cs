﻿using MapMaven.Core.Models.Data;
using MapMaven.Core.Services;
using MapMaven.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace MapMaven.Infrastructure.Data
{
    public class MapMavenContext : DbContext, IDataStore
    {
        public DbSet<MapInfo> MapInfos { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<HiddenMap> HiddenMaps { get; set; }
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }

        public static string DbPath => Path.Join(BeatSaberFileService.AppDataLocation, "MapMaven.db");

        public MapMavenContext() { }

        public MapMavenContext(DbContextOptions<MapMavenContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured)
                return;

            if (!Directory.Exists(BeatSaberFileService.AppDataLocation))
                Directory.CreateDirectory(BeatSaberFileService.AppDataLocation);

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
