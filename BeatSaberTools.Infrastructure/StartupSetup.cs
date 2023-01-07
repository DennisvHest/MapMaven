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

            var context = scope.ServiceProvider.GetService<BSToolsContext>();

            context.Database.Migrate();
        }
    }
}