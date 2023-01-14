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
        public static void AddBeatSaberTools<TFileService>(this IServiceCollection services) where TFileService : BeatSaverFileServiceBase
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

            services.AddScoped(typeof(BeatSaverFileServiceBase), typeof(TFileService));
            services.AddScoped<BeatSaberDataService>();
            services.AddScoped<MapService>();
            services.AddScoped<SongPlayerService>();
            services.AddScoped<PlaylistService>();
            services.AddScoped<ScoreSaberService>();
            services.AddScoped<DynamicPlaylistArrangementService>();
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<BSToolsContext>();

            context.Database.Migrate();

            SetDbFullAccessPermissions();
        }

        /// <summary>
        /// Sets full access permissions on the SQLite db file to avoid "writing to readonly database" error
        /// </summary>
        private static void SetDbFullAccessPermissions()
        {
            var dbFileInfo = new FileInfo(BSToolsContext.DbPath);
            var access = dbFileInfo.GetAccessControl();

            var accessRule = new FileSystemAccessRule(
                identity: new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                fileSystemRights: FileSystemRights.FullControl,
                inheritanceFlags: InheritanceFlags.None,
                propagationFlags: PropagationFlags.NoPropagateInherit,
                type: AccessControlType.Allow
            );

            access.AddAccessRule(accessRule);
            dbFileInfo.SetAccessControl(access);
        }
    }
}