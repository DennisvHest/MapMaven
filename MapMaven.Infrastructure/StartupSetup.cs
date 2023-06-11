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

namespace MapMaven.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddMapMaven(this IServiceCollection services)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            services.AddDbContext<MapMavenContext>();
            services.AddScoped<IDataStore, MapMavenContext>();

            services.AddScoped<IBeatmapHasher, Hasher>();
            services.AddScoped(_ => new BeatSaver("MapMaven", new Version(1, 0)));

            services.AddHttpClient<ScoreSaberApiClient>();
            services.AddHttpClient("RankedScoresaber", config =>
            {
                config.BaseAddress = new Uri("https://scoresaber.balibalo.xyz");
            });

            services.AddSingleton<BeatSaberFileService>();
            services.AddSingleton<IBeatSaberDataService, BeatSaberDataService>();
            services.AddSingleton<IMapService, MapService>();
            services.AddSingleton<SongPlayerService>();
            services.AddSingleton<IPlaylistService, PlaylistService>();
            services.AddSingleton<IScoreSaberService, ScoreSaberService>();
            services.AddSingleton<DynamicPlaylistArrangementService>();
            services.AddSingleton<IApplicationSettingService, ApplicationSettingService>();
            services.AddSingleton<ApplicationEventService>();
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