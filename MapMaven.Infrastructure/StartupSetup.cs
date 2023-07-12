using BeatSaber.SongHashing;
using MapMaven.Core.ApiClients;
using MapMaven.Core.Services;
using MapMaven.Infrastructure.Data;
using MapMaven.Services;
using BeatSaverSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using MapMaven.Core.Services.Interfaces;
using Newtonsoft.Json;
using System.IO.Abstractions;

namespace MapMaven.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddMapMaven(this IServiceCollection services)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            JsonConvert.DefaultSettings = () => new() { DateParseHandling = DateParseHandling.None };

            services.AddDbContext<MapMavenContext>();
            services.AddScoped<IDataStore, MapMavenContext>();

            services.AddScoped<IFileSystem, FileSystem>();

            services.AddScoped<IBeatmapHasher, Hasher>();
            services.AddScoped(_ => new BeatSaver("MapMaven", new Version(1, 0)));

            services.AddHttpClient<ScoreSaberApiClient>();
            services.AddHttpClient("RankedScoresaber", config =>
            {
                config.BaseAddress = new Uri("https://scoresaber.balibalo.xyz");
            });

            services.AddScoped<BeatSaberFileService>();
            services.AddScoped<IBeatSaberDataService, BeatSaberDataService>();
            services.AddScoped<IMapService, MapService>();
            services.AddScoped<SongPlayerService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IScoreSaberService, ScoreSaberService>();
            services.AddScoped<DynamicPlaylistArrangementService>();
            services.AddScoped<IApplicationSettingService, ApplicationSettingService>();
            services.AddScoped<IApplicationEventService, ApplicationEventService>();
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<MapMavenContext>();

            context.Database.Migrate();

            SetDbFullAccessPermissions();
        }

        /// <summary>
        /// Sets full access permissions on the application data directory to avoid "writing to readonly database" error
        /// </summary>
        private static void SetDbFullAccessPermissions()
        {
            var appDataDirectoryInfo = new DirectoryInfo(BeatSaberFileService.AppDataLocation);
            var access = appDataDirectoryInfo.GetAccessControl();

            var accessRule = new FileSystemAccessRule(
                identity: new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                fileSystemRights: FileSystemRights.FullControl,
                inheritanceFlags: InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                propagationFlags: PropagationFlags.None,
                type: AccessControlType.Allow
            );

            access.AddAccessRule(accessRule);
            appDataDirectoryInfo.SetAccessControl(access);
        }
    }
}