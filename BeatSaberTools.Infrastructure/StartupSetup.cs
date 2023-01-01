using BeatSaber.SongHashing;
using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Services;
using BeatSaverSharp;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BeatSaberTools.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddBeatSaberTools<TFileService>(this IServiceCollection services) where TFileService : IBeatSaverFileService
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            services.AddSingleton<IBeatmapHasher, Hasher>();
            services.AddSingleton(_ => new BeatSaver("BeatSaberTools", new Version(1, 0)));

            services.AddHttpClient<ScoreSaberApiClient>();
            services.AddHttpClient("RankedScoresaber", config =>
            {
                config.BaseAddress = new Uri("https://scoresaber.balibalo.xyz");
            });

            services.AddSingleton(typeof(IBeatSaverFileService), typeof(TFileService));
            services.AddSingleton<BeatSaberDataService>();
            services.AddSingleton<MapService>();
            services.AddSingleton<SongPlayerService>();
            services.AddSingleton<PlaylistService>();
            services.AddSingleton<ScoreSaberService>();
            services.AddSingleton<DynamicPlaylistArrangementService>();
        }
    }
}