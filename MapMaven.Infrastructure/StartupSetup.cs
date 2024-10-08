﻿using BeatSaber.SongHashing;
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
using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Services.Leaderboards.ScoreEstimation;

namespace MapMaven.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddMapMaven(this IServiceCollection services, bool useStatefulServices = false)
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
            services.AddHttpClient<BeatLeaderApiClient>(client => client.BaseAddress = new Uri("https://api.beatleader.xyz"));
            services.AddHttpClient<BeatSaverApiClient>(client => client.BaseAddress = new Uri("https://api.beatsaver.com"));
            services.AddHttpClient("GithubApi", client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/repos/DennisvHest/MapMaven/");
                client.DefaultRequestHeaders.Add("User-Agent", "MapMaven");
            });
            services.AddHttpClient("MapMavenFiles", client =>
            {
#if DEBUG
                client.BaseAddress = new Uri("https://mapmavenstoragetest.z6.web.core.windows.net");
#else
                client.BaseAddress = new Uri("http://files.map-maven.com");
#endif
            });

            var serviceScope = useStatefulServices ? ServiceLifetime.Singleton : ServiceLifetime.Scoped;

            services.Add(new ServiceDescriptor(typeof(BeatSaberFileService), typeof(BeatSaberFileService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IBeatSaberDataService), typeof(BeatSaberDataService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IMapService), typeof(MapService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(SongPlayerService), typeof(SongPlayerService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IPlaylistService), typeof(PlaylistService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ILeaderboardService), typeof(LeaderboardService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ScoreSaberService), typeof(ScoreSaberService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(BeatLeaderService), typeof(BeatLeaderService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ILeaderboardProviderService), services => services.GetRequiredService<ScoreSaberService>(), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ILeaderboardProviderService), services => services.GetRequiredService<BeatLeaderService>(), serviceScope));
            services.Add(new ServiceDescriptor(typeof(LivePlaylistArrangementService), typeof(LivePlaylistArrangementService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IApplicationSettingService), typeof(ApplicationSettingService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IApplicationEventService), typeof(ApplicationEventService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ILeaderboardDataService), typeof(LeaderboardDataService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IScoreEstimationService), typeof(ScoreSaberScoreEstimationService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(IScoreEstimationService), typeof(BeatLeaderScoreEstimationService), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ScoreEstimationSettings), typeof(ScoreEstimationSettings), serviceScope));
            services.Add(new ServiceDescriptor(typeof(ApplicationFilesService), typeof(ApplicationFilesService), serviceScope));
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