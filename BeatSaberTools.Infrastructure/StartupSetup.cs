using BeatSaber.SongHashing;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BeatSaberTools.Infrastructure
{
    public static class StartupSetup
    {
        public static void AddBeatSaberTools<TFileService>(this IServiceCollection services) where TFileService : IBeatSaverFileService
        {
            services.AddSingleton<IBeatmapHasher, Hasher>();

            services.AddSingleton(typeof(IBeatSaverFileService), typeof(TFileService));
            services.AddSingleton<BeatSaberDataService>();
            services.AddSingleton<MapService>();
            services.AddSingleton<SongPlayerService>();
            services.AddSingleton<PlaylistService>();
        }
    }
}