using BeatSaber.SongHashing;
using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Infrastructure.Data;
using BeatSaberTools.Services;
using BeatSaverSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;

namespace BeatSaberTools.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddBeatSaberTools(this IServiceCollection services)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            services.AddDbContext<BSToolsContext>();
            services.AddScoped<IDataStore, BSToolsContext>();

            services.AddScoped<IBeatmapHasher, Hasher>();
            services.AddScoped(_ => new BeatSaver("BeatSaberTools", new Version(1, 0)));

            services.AddHttpClient<ScoreSaberApiClient>();
            services.AddHttpClient("RankedScoresaber", config =>
            {
                config.BaseAddress = new Uri("https://scoresaber.balibalo.xyz");
            });

            services.AddScoped<BeatSaverFileService>();
            services.AddScoped<BeatSaberDataService>();
            services.AddScoped<MapService>();
            services.AddScoped<SongPlayerService>();
            services.AddScoped<PlaylistService>();
            services.AddScoped<ScoreSaberService>();
            services.AddScoped<DynamicPlaylistArrangementService>();
            services.AddScoped<ApplicationSettingService>();
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<BSToolsContext>();

            context.Database.Migrate();

            SetDbFullAccessPermissions();
        }

        /// <summary>
        /// Sets full access permissions on the application data directory to avoid "writing to readonly database" error
        /// </summary>
        private static void SetDbFullAccessPermissions()
        {
            var appDataDirectoryInfo = new DirectoryInfo(BeatSaverFileService.AppDataLocation);
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