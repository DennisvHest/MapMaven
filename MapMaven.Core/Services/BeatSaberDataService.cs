using MapMaven.Models.Data;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.RegularExpressions;
using NVorbis;
using BeatSaber.SongHashing;
using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics;
using Image = System.Drawing.Image;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using BeatSaberPlaylistsLib.Legacy;
using MapMaven.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;

namespace MapMaven.Services
{
    public class BeatSaberDataService
    {
        private readonly BeatSaberFileService _fileService;

        private readonly IBeatmapHasher _beatmapHasher;
        private PlaylistManager _playlistManager;

        private readonly IServiceProvider _serviceProvider;

        private readonly Regex _mapIdRegex = new Regex(@"^[0-9A-Fa-f]+");

        private readonly BehaviorSubject<Dictionary<string, MapInfo>> _mapInfo = new(new Dictionary<string, MapInfo>());
        private readonly BehaviorSubject<bool> _loadingMapInfo = new(false);

        private readonly BehaviorSubject<IEnumerable<IPlaylist>> _playlistInfo = new(Array.Empty<IPlaylist>());
        private readonly BehaviorSubject<bool> _loadingPlaylistInfo = new(false);

        public IObservable<Dictionary<string, MapInfo>> MapInfoByHash => _mapInfo;
        public IObservable<IEnumerable<MapInfo>> MapInfo => _mapInfo.Select(x => x.Values);
        public IObservable<bool> LoadingMapInfo => _loadingMapInfo;

        public IObservable<IEnumerable<IPlaylist>> PlaylistInfo => _playlistInfo;
        public IObservable<bool> LoadingPlaylistInfo => _loadingPlaylistInfo;

        public BeatSaberDataService(IBeatmapHasher beatmapHasher, BeatSaberFileService fileService, IServiceProvider serviceProvider)
        {
            _fileService = fileService;
            _beatmapHasher = beatmapHasher;
            _serviceProvider = serviceProvider;

            _fileService.PlaylistsLocationObservable.Subscribe(playlistsLocation =>
            {
                _playlistManager = new PlaylistManager(_fileService.PlaylistsLocation, new LegacyPlaylistHandler());
            });

            _fileService.BeatSaberInstallLocationObservable
                .DistinctUntilChanged()
                .Select(_ => Observable.FromAsync(LoadAllMapInfo))
                .Concat()
                .Subscribe();
        }

        public async Task LoadAllMapInfo()
        {
            _loadingMapInfo.OnNext(true);

            try
            {
                IEnumerable<MapInfo> mapInfo = await GetAllMapInfo();

                // Create a dictionary grouping all map info by ID
                var mapInfoDictionary = mapInfo
                    .GroupBy(i => i.Hash)
                    .Select(g => g.First())
                    .ToDictionary(i => i.Hash);

                await CacheMapInfo(mapInfoDictionary);

                _mapInfo.OnNext(mapInfoDictionary);
            }
            finally
            {
                _loadingMapInfo.OnNext(false);
            }
        }

        public async Task LoadMapInfo(string id)
        {
            var mapDirectory = Directory.EnumerateDirectories(_fileService.MapsLocation)
                .FirstOrDefault(d =>
                {
                    var directoryName = Path.GetFileName(d);

                    return directoryName.StartsWith(id);
                });

            if (mapDirectory == null)
                return;

            var mapInfo = await GetMapInfo(mapDirectory);

            if (_mapInfo.Value.ContainsKey(mapInfo.Hash))
                return;

            _mapInfo.Value.Add(mapInfo.Hash, mapInfo);

            _mapInfo.OnNext(_mapInfo.Value);
        }

        public async Task<IEnumerable<MapInfo>> GetAllMapInfo()
        {
            if (!Directory.Exists(_fileService.MapsLocation))
                return Enumerable.Empty<MapInfo>();

            var songHashData = await GetSongHashData();
            var mapInfoCache = await GetMapInfoCache();

            var fileReadTasks = Directory.EnumerateDirectories(_fileService.MapsLocation)
                .Select(mapDirectory => GetMapInfo(mapDirectory, songHashData, mapInfoCache));

            var mapInfo = await Task.WhenAll(fileReadTasks);

            return mapInfo.Where(i => i != null);
        }

        public async Task LoadAllPlaylists()
        {
            _loadingPlaylistInfo.OnNext(true);

            try
            {
                var playlists = await GetAllPlaylists();

                _playlistInfo.OnNext(playlists);
            }
            finally
            {
                _loadingPlaylistInfo.OnNext(false);
            }
        }

        public async Task<IEnumerable<IPlaylist>> GetAllPlaylists()
        {
            _playlistManager.RefreshPlaylists(false);

            return _playlistManager.GetAllPlaylists();
        }

        /// <summary>
        /// Gets the hash data from SongCore (pre-generated hashes). Required to identify newly added or existing maps for caching.
        /// </summary>
        private async Task<Dictionary<string, SongHash>> GetSongHashData()
        {
            var songHashFilePath = Path.Combine(_fileService.UserDataLocation, "SongCore", "SongHashData.dat");

            if (!File.Exists(songHashFilePath))
                return new Dictionary<string, SongHash>();

            var songHashJson = await File.ReadAllTextAsync(songHashFilePath);

            var songHashData = JsonSerializer.Deserialize<Dictionary<string, SongHash>>(songHashJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return songHashData.ToDictionary(x => x.Key.NormalizePath(), x => x.Value);
        }

        /// <summary>
        /// Gets all the information of the map located in the given mapDirectory.
        /// </summary>
        /// <param name="mapDirectory">The directory path of the map.</param>
        /// <param name="songHashData">All song hashes.</param>
        /// <param name="mapInfoCache">MapInfo from the cache.</param>
        private async Task<MapInfo> GetMapInfo(string mapDirectory, Dictionary<string, SongHash>? songHashData = null, Dictionary<string, MapInfo>? mapInfoCache = null)
        {
            if (songHashData == null)
                songHashData = new();

            if (mapInfoCache == null)
                mapInfoCache = new();

            var mapHash = songHashData.GetValueOrDefault(mapDirectory.NormalizePath())?.Hash;

            Debug.WriteLine($"Found hash for {mapDirectory}");

            if (mapHash == null)
            {
                Debug.WriteLine($"Dit not find hash for {mapDirectory}");

                var hashResult = await _beatmapHasher.HashDirectoryAsync(mapDirectory, new CancellationToken());

                if (hashResult.ResultType != HashResultType.Success)
                    return null;

                mapHash = hashResult.Hash;
            }

            if (mapInfoCache.TryGetValue(mapHash, out var info))
            {
                // Map info was found in cache. No further data retrieval nescessary.
                return info;
            }
            else
            {
                var infoFilePath = Path.Combine(mapDirectory, "Info.dat");
                var mapInfoText = await File.ReadAllTextAsync(infoFilePath);

                info = JsonSerializer.Deserialize<MapInfo>(mapInfoText);
            }

            info.Hash = mapHash;
            info.DirectoryPath = mapDirectory;

            var directoryName = Path.GetFileName(Path.GetDirectoryName(info.DirectoryPath + "/"));

            info.Id = _mapIdRegex.Match(directoryName)?.Value;

            if (string.IsNullOrEmpty(info.Id))
                return null;

            var mapDirectoryInfo = new DirectoryInfo(mapDirectory);

            info.AddedDateTime = mapDirectoryInfo.CreationTime;

            FillSongInfo(info);

            return info;
        }

        /// <summary>
        /// Fills additional song info of the given map that is not found within the Info.dat file of the map.
        /// </summary>
        private void FillSongInfo(MapInfo info)
        {
            var audioFilePath = Path.Combine(info.DirectoryPath, info.SongFileName);

            using (var audioFile = new VorbisReader(audioFilePath))
            {
                info.SongDuration = audioFile.TotalTime;
            }
        }

        public Image GetMapCoverImage(string mapId)
        {
            var mapInfo = _mapInfo.Value[mapId];

            var imageFilePath = Path.Combine(mapInfo.DirectoryPath, mapInfo.CoverImageFilename);

            return Image.FromFile(imageFilePath);
        }

        public string GetMapSongPath(string mapId)
        {
            var mapInfo = _mapInfo.Value[mapId];

            return Path.Combine(mapInfo.DirectoryPath, mapInfo.SongFileName);
        }

        /// <summary>
        /// Returns the MapInfo from the cache.
        /// This is a dictionary with the map hash as the key.
        /// </summary>
        private async Task<Dictionary<string, MapInfo>> GetMapInfoCache()
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetService<IDataStore>();

            var mapInfo = await dataStore.Set<MapInfo>().ToListAsync();

            return mapInfo.ToDictionary(i => i.Hash);
        }

        /// <summary>
        /// Writes the given MapInfo's as JSON to the AppData cache.
        /// The MapInfo's are transformed to a dictionary with the hash as the key.
        /// </summary>
        private async Task CacheMapInfo(Dictionary<string, MapInfo> mapInfoByHash)
        {
            if (!mapInfoByHash.Any())
                return;

            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetService<IDataStore>();

            dataStore.Set<MapInfo>().RemoveRange(dataStore.Set<MapInfo>());
            dataStore.Set<MapInfo>().AddRange(mapInfoByHash.Values);

            await dataStore.SaveChangesAsync();
        }
    }
}
