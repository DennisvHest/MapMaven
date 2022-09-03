using BeatSaberTools.Models;
using BeatSaberTools.Services;
using System.Diagnostics;

namespace BeatSaberTools.Worker
{
    public class Worker : BackgroundService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly PlaylistService _playlistService;

        private readonly PeriodicTimer _timer = new PeriodicTimer(Debugger.IsAttached ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(5));

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, BeatSaberDataService beatSaberDataService, PlaylistService playlistService)
        {
            _logger = logger;
            _beatSaberDataService = beatSaberDataService;
            _playlistService = playlistService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            do
            {
                _logger.LogInformation("Gathering recently added maps...");

                var playlists = await _beatSaberDataService.GetAllPlaylists();

                var dynamicPlaylists = playlists
                    .Select(p => new
                    {
                        Playlist = new Playlist(p),
                        PlaylistInfo = p
                    })
                    .Where(x => x.Playlist.IsDynamicPlaylist);

                if (!dynamicPlaylists.Any())
                    continue;

                var maps = await _beatSaberDataService.GetAllMapInfo();

                var recentlyAddedMaps = maps
                    .OrderByDescending(m => m.AddedDateTime)
                    .Take(10)
                    .Select(m => m.ToMap());

                foreach (var playlist in dynamicPlaylists)
                {
                    await _playlistService.ReplaceMapsInPlaylist(recentlyAddedMaps, playlist.Playlist, loadPlaylists: false);
                }

                _logger.LogInformation("Created playlist with recently added maps!");
            }
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested);
        }
    }
}