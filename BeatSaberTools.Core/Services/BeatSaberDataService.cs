using BeatSaberTools.Models.Data;
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
using BeatSaberTools.Core.Services;

namespace BeatSaberTools.Services
{
    public class BeatSaberDataService
    {
        private readonly IBeatSaverFileService _fileService;

        private readonly IBeatmapHasher _beatmapHasher;
        private readonly PlaylistManager _playlistManager;

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

        public BeatSaberDataService(IBeatmapHasher beatmapHasher, IBeatSaverFileService fileService)
        {
            _fileService = fileService;
            _beatmapHasher = beatmapHasher;

            _playlistManager = new PlaylistManager(_fileService.PlaylistsLocation, new LegacyPlaylistHandler());
        }

        public async Task LoadAllMapInfo()
        {
            _loadingMapInfo.OnNext(true);

            try
            {
                var songHashData = await GetSongHashData();
                var mapInfoCache = await GetMapInfoCache();

                var fileReadTasks = Directory.EnumerateDirectories(_fileService.MapsLocation)
                    .Select(mapDirectory => GetMapInfo(mapDirectory, songHashData, mapInfoCache));

                IEnumerable<MapInfo> mapInfo = await Task.WhenAll(fileReadTasks);

                // Create a dictionary grouping all map info by ID
                var mapInfoDictionary = mapInfo
                    .Where(i => i != null)
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

        public async Task LoadAllPlaylists()
        {
            _loadingPlaylistInfo.OnNext(true);

            try
            {
                _playlistManager.RefreshPlaylists(false);
                var playlists = _playlistManager.GetAllPlaylists();

                _playlistInfo.OnNext(playlists);
            }
            finally
            {
                _loadingPlaylistInfo.OnNext(false);
            }
        }

        /// <summary>
        /// Gets the hash data from SongCore (pre-generated hashes). Required to identify newly added or existing maps for caching.
        /// </summary>
        private async Task<Dictionary<string, SongHash>> GetSongHashData()
        {
            var songHashFilePath = Path.Combine(_fileService.UserDataLocation, "SongCore", "SongHashData.dat");
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
        private async Task<MapInfo> GetMapInfo(string mapDirectory, Dictionary<string, SongHash> songHashData, Dictionary<string, MapInfo> mapInfoCache)
        {
            var mapHash = songHashData.GetValueOrDefault(mapDirectory.NormalizePath())?.Hash;

            Debug.WriteLine($"Found hash for {mapDirectory}");

            if (mapHash == null)
            {
                Debug.WriteLine($"Dit not find hash for {mapDirectory}");

                var hashResult = _beatmapHasher.HashDirectory(mapDirectory, new CancellationToken());

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
            Dictionary<string, MapInfo> mapInfoCache = new Dictionary<string, MapInfo>();

            if (File.Exists(_fileService.MapInfoCachePath))
            {
                using (var mapInfoCacheStream = File.OpenRead(_fileService.MapInfoCachePath))
                {
                    mapInfoCache = await JsonSerializer.DeserializeAsync<Dictionary<string, MapInfo>>(mapInfoCacheStream);
                }
            }

            return mapInfoCache;
        }

        /// <summary>
        /// Writes the given MapInfo's as JSON to the AppData cache.
        /// The MapInfo's are transformed to a dictionary with the hash as the key.
        /// </summary>
        private async Task CacheMapInfo(Dictionary<string, MapInfo> mapInfoByHash)
        {
            string mapInfoCacheJson = JsonSerializer.Serialize(mapInfoByHash);

            await File.WriteAllTextAsync(_fileService.MapInfoCachePath, mapInfoCacheJson);
        }
    }
}
