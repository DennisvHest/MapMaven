using AspNetCore.UnitOfWork;
using BeatSaber.SongHashing;
using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Infrastructure.Data;
using BeatSaberTools.Services;
using BeatSaverSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BeatSaberTools.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddBeatSaberTools<TFileService>(this IServiceCollection services) where TFileService : BeatSaverFileServiceBase
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            services.AddDbContext<BSToolsContext>()
                .AddUnitOfWork<BSToolsContext>();

            services.AddSingleton<IBeatmapHasher, Hasher>();
            services.AddSingleton(_ => new BeatSaver("BeatSaberTools", new Version(1, 0)));

            services.AddHttpClient<ScoreSaberApiClient>();
            services.AddHttpClient("RankedScoresaber", config =>
            {
                config.BaseAddress = new Uri("https://scoresaber.balibalo.xyz");
            });

            services.AddSingleton(typeof(BeatSaverFileServiceBase), typeof(TFileService));
            services.AddSingleton<BeatSaberDataService>();
            services.AddSingleton<MapService>();
            services.AddSingleton<SongPlayerService>();
            services.AddSingleton<PlaylistService>();
            services.AddSingleton<ScoreSaberService>();
            services.AddSingleton<DynamicPlaylistArrangementService>();
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetService<BSToolsContext>();

            context.Database.Migrate();
        }
    }
}