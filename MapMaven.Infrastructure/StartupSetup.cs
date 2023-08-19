using BeatSaber.SongHashing;
using MapMaven.Core.ApiClients.ScoreSaber;
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
using MapMaven.Core.OpenAPI;
using MapMaven.Core.ApiClients.BeatSaver;

namespace MapMaven.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddMapMaven(this IServiceCollection services)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            JsonConvert.DefaultSettings = () => new() {
                DateParseHandling = DateParseHandling.None,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new SafeContractResolver()
            };

            services.AddDbContext<MapMavenContext>();
            services.AddScoped<IDataStore, MapMavenContext>();

            services.AddSingleton<IFileSystem, FileSystem>();

            services.AddScoped<IBeatmapHasher, Hasher>();
            services.AddScoped(_ => new BeatSaver("MapMaven", new Version(1, 0)));

            services.AddHttpClient<ScoreSaberApiClient>(client => client.BaseAddress = new Uri("https://scoresaber.com"));
            services.AddHttpClient<BeatSaverApiClient>(client => client.BaseAddress = new Uri("https://api.beatsaver.com"));
            services.AddHttpClient("MapMavenFiles", client => client.BaseAddress = new Uri("http://files.map-maven.com"));

            services.AddSingleton<BeatSaberFileService>();
            services.AddSingleton<IBeatSaberDataService, BeatSaberDataService>();
            services.AddSingleton<IMapService, MapService>();
            services.AddSingleton<SongPlayerService>();
            services.AddSingleton<IPlaylistService, PlaylistService>();
            services.AddSingleton<IScoreSaberService, ScoreSaberService>();
            services.AddSingleton<DynamicPlaylistArrangementService>();
            services.AddSingleton<IApplicationSettingService, ApplicationSettingService>();
            services.AddSingleton<IApplicationEventService, ApplicationEventService>();
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