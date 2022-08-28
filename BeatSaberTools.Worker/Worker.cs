using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Models;
using BeatSaberTools.Services;

namespace BeatSaberTools.Worker
{
    public class Worker : BackgroundService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly PlaylistManager _playlistManager;

        private readonly IBeatSaverFileService _beatSaverFileService;

        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, BeatSaberDataService beatSaberDataService, IBeatSaverFileService beatSaverFileService)
        {
            _logger = logger;
            _beatSaberDataService = beatSaberDataService;
            _beatSaverFileService = beatSaverFileService;
            _playlistManager = new PlaylistManager(_beatSaverFileService.PlaylistsLocation, new LegacyPlaylistHandler());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync() && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Gathering recently added maps...");

                var maps = await _beatSaberDataService.GetAllMapInfo();

                var recentlyAddedMaps = maps.OrderByDescending(m => m.AddedDateTime).Take(10);

                var addedPlaylist = _playlistManager.CreatePlaylist(
                    fileName: "test",
                    title: "test",
                    author: "Beat Saber Tools",
                    coverImage: null
                 );

                foreach (var map in recentlyAddedMaps)
                {
                    addedPlaylist.Add(
                        songHash: map.Hash,
                        songName: map.SongName,
                        mapper: map.LevelAuthorName,
                        songKey: null
                    );
                }

                _playlistManager.StorePlaylist(addedPlaylist);

                _logger.LogInformation("Created playlist with recently added maps!");
            }
        }
    }
}