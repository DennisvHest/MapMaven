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
using Microsoft.Extensions.Logging;
using MapMaven.Core.Models.Data;
using System.Runtime;
using MapMaven.Core.Services.Interfaces;
using System.IO.Abstractions;
using MapMaven.Core.Models.Data.Playlists;

namespace MapMaven.Services
{
    public class BeatSaberDataService : IBeatSaberDataService
    {
        private readonly BeatSaberFileService _fileService;

        private readonly IBeatmapHasher _beatmapHasher;
        public PlaylistManager PlaylistManager { get; private set; }

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<IBeatSaberDataService> _logger;

        private readonly IFileSystem _fileSystem;

        private readonly Regex _mapIdRegex = new Regex(@"^[0-9A-Fa-f]+");

        private readonly BehaviorSubject<Dictionary<string, MapInfo>> _mapInfo = new(new Dictionary<string, MapInfo>());
        private readonly BehaviorSubject<bool> _loadingMapInfo = new(false);
        private readonly BehaviorSubject<bool> _initialMapLoad = new(false);

        private readonly BehaviorSubject<PlaylistTree<IPlaylist>?> _playlistTree = new(null);
        private readonly BehaviorSubject<bool> _loadingPlaylistInfo = new(false);

        public IObservable<Dictionary<string, MapInfo>> MapInfoByHash => _mapInfo;
        public IObservable<IEnumerable<MapInfo>> MapInfo => _mapInfo.Select(x => x.Values);
        public IObservable<bool> LoadingMapInfo => _loadingMapInfo;
        public IObservable<bool> InitialMapLoad => _initialMapLoad;

        public IObservable<IEnumerable<IPlaylist>> PlaylistInfo { get; private set; }
        public IObservable<bool> LoadingPlaylistInfo => _loadingPlaylistInfo;

        public BeatSaberDataService(IBeatmapHasher beatmapHasher, BeatSaberFileService fileService, IServiceProvider serviceProvider, ILogger<BeatSaberDataService> logger, IFileSystem fileSystem)
        {
            _fileService = fileService;
            _beatmapHasher = beatmapHasher;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _fileSystem = fileSystem;

            _fileService.PlaylistsLocationObservable.Subscribe(playlistsLocation =>
            {
                PlaylistManager = new PlaylistManager(_fileService.PlaylistsLocation, new LegacyPlaylistHandler());
            });

            _fileService.BeatSaberInstallLocationObservable
                .DistinctUntilChanged()
                .Select(_ => Observable.FromAsync(LoadAllMapInfo))
                .Concat()
                .Subscribe();

            PlaylistInfo = _playlistTree.Select(tree => tree?.AllPlaylists() ?? []);
        }

        public async Task LoadAllMapInfo()
        {
            _loadingMapInfo.OnNext(true);

            try
            {
                IEnumerable<MapInfo> mapInfo = await GetAllMapInfo();

                // Create a dictionary grouping all map info by ID
                var mapInfoDictionary = mapInfo
                    .GroupBy(i => i.Id)
                    .Select(g => g.OrderByDescending(i => i.AddedDateTime).First())
                    .GroupBy(i => i.Hash)
                    .Select(g => g.First())
                    .ToDictionary(i => i.Hash);

                await CacheMapInfo(mapInfoDictionary);

                _mapInfo.OnNext(mapInfoDictionary);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while loading all map info.");
                throw;
            }
            finally
            {
                _loadingMapInfo.OnNext(false);
                _initialMapLoad.OnNext(false);
            }
        }

        public async Task LoadMapInfo(string id)
        {
            var mapDirectory = _fileSystem.Directory.EnumerateDirectories(_fileService.MapsLocation)
                .FirstOrDefault(d =>
                {
                    var directoryName = _fileSystem.Path.GetFileName(d);

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
            if (!_fileSystem.Directory.Exists(_fileService.MapsLocation))
                return Enumerable.Empty<MapInfo>();

            var songHashData = await GetSongHashData();
            var songDurationCache = await GetSongDurationCache();
            var mapInfoCache = await GetMapInfoCache();

            var mapsByHash = mapInfoCache.ToDictionary(i => i.Hash);
            var mapsByDirectoryPath = mapInfoCache.ToDictionary(i => i.DirectoryPath.NormalizePath());

            var fileReadTasks = _fileSystem.Directory.EnumerateDirectories(_fileService.MapsLocation)
                .Select(mapDirectory => GetMapInfo(mapDirectory, songHashData, mapsByHash, mapsByDirectoryPath, songDurationCache));

            var mapInfo = await Task.WhenAll(fileReadTasks);

            return mapInfo.Where(i => i != null);
        }

        public async Task LoadAllPlaylists()
        {
            _loadingPlaylistInfo.OnNext(true);

            try
            {
                var playlistTree = await GetPlaylistTree();

                _playlistTree.OnNext(playlistTree);
            }
            finally
            {
                _loadingPlaylistInfo.OnNext(false);
            }
        }

        public async Task<IEnumerable<IPlaylist>> GetAllPlaylists()
        {
            var playlistTree = await GetPlaylistTree();

            return playlistTree.AllPlaylists();
        }

        public async Task<PlaylistTree<IPlaylist>> GetPlaylistTree()
        {
            PlaylistManager.RefreshPlaylists(true);

            var tree = new PlaylistTree<IPlaylist>(PlaylistManager);

            AddPlaylistManagerPlaylistsToFolder(tree.RootPlaylistFolder);

            CleanLargeObjectHeap();

            return tree;
        }

        private void AddPlaylistManagerPlaylistsToFolder(PlaylistFolder<IPlaylist> folder)
        {
            foreach (var childPlaylistManager in folder.PlaylistManager.GetChildManagers())
            {
                var childFolder = new PlaylistFolder<IPlaylist>(childPlaylistManager);
                folder.ChildItems.Add(childFolder);

                AddPlaylistManagerPlaylistsToFolder(childFolder);
            }

            var playlists = folder.PlaylistManager.GetAllPlaylists();

            foreach (var playlist in playlists)
            {
                folder.ChildItems.Add(new PlaylistTreeNode<IPlaylist>(playlist, folder.PlaylistManager));
            }
        }

        /// <summary>
        /// Gets the hash data from SongCore (pre-generated hashes). Required to identify newly added or existing maps for caching.
        /// </summary>
        private async Task<Dictionary<string, SongHash>> GetSongHashData()
        {
            try
            {
                var songHashFilePath = _fileSystem.Path.Combine(_fileService.UserDataLocation, "SongCore", "SongHashData.dat");

                if (!_fileSystem.File.Exists(songHashFilePath))
                    return new Dictionary<string, SongHash>();

                var songHashJson = await _fileSystem.File.ReadAllTextAsync(songHashFilePath);

                var songHashData = JsonSerializer.Deserialize<Dictionary<string, SongHash>>(songHashJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return songHashData.ToDictionary(x => x.Key.NormalizePath(), x => x.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading song hash data.");

                return new();
            }
        }

        /// <summary>
        /// Source: https://github.com/Kylemc1413/SongCore
        /// </summary>
        public string GetRelativeMapPath(string path)
        {
            var fromPath = _fileService.BeatSaberInstallLocation.NormalizePath();

            if (!fromPath.EndsWith(_fileSystem.Path.DirectorySeparatorChar.ToString()))
            {
                fromPath += _fileSystem.Path.DirectorySeparatorChar;
            }

            if (!path.StartsWith(fromPath)) return path;

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(path);

            var relativePath = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());

            if (!relativePath.StartsWith("."))
            {
                relativePath = _fileSystem.Path.Combine(".", relativePath);
            }

            return relativePath.Replace(_fileSystem.Path.AltDirectorySeparatorChar, _fileSystem.Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the song duration cache from SongCore.
        /// </summary>
        private async Task<Dictionary<string, SongDuration>> GetSongDurationCache()
        {
            try
            {
                var songHashFilePath = _fileSystem.Path.Combine(_fileService.UserDataLocation, "SongCore", "SongDurationCache.dat");

                if (!_fileSystem.File.Exists(songHashFilePath))
                    return new Dictionary<string, SongDuration>();

                var songDurationJson = await _fileSystem.File.ReadAllTextAsync(songHashFilePath);

                var songDurationData = JsonSerializer.Deserialize<Dictionary<string, SongDuration>>(songDurationJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return songDurationData.ToDictionary(x => x.Key.NormalizePath(), x => x.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading song duration cache.");

                return new();
            }
        }

        /// <summary>
        /// Gets all the information of the map located in the given mapDirectory.
        /// </summary>
        /// <param name="mapDirectory">The directory path of the map.</param>
        /// <param name="songHashData">All song hashes.</param>
        /// <param name="mapsByHashCache">MapInfo from the cache.</param>
        private async Task<MapInfo> GetMapInfo(
            string mapDirectory,
            Dictionary<string, SongHash>? songHashData = null,
            Dictionary<string, MapInfo>? mapsByHashCache = null,
            Dictionary<string, MapInfo>? mapsByDirectoryCache = null,
            Dictionary<string, SongDuration>? songDurationCache = null)
        {
            try
            {
                if (songHashData == null)
                    songHashData = new();

                if (mapsByHashCache == null)
                    mapsByHashCache = new();

                if (mapsByDirectoryCache == null)
                    mapsByDirectoryCache = new();

                if (songDurationCache == null)
                    songDurationCache = new();

                var normalizedMapDirectory = mapDirectory.NormalizePath();

                var mapHash = mapsByDirectoryCache.GetValueOrDefault(normalizedMapDirectory)?.Hash;

                if (mapHash == null)
                {
                    // Check if hash is found in SongCore cache (from relative map directory)
                    var relativeMapDirectory = GetRelativeMapPath(normalizedMapDirectory);

                    mapHash = songHashData.GetValueOrDefault(relativeMapDirectory)?.Hash;
                }

                if (mapHash == null) // Check if hash is found in SongCore cache (from absolute map directory (legacy))
                    mapHash = songHashData.GetValueOrDefault(normalizedMapDirectory)?.Hash;

                if (mapHash == null)
                {
                    Debug.WriteLine($"Dit not find hash for {normalizedMapDirectory}");

                    var hashResult = await _beatmapHasher.HashDirectoryAsync(normalizedMapDirectory, new CancellationToken());

                    if (hashResult.ResultType != HashResultType.Success)
                        return null;

                    mapHash = hashResult.Hash;
                }

                if (mapsByHashCache.TryGetValue(mapHash, out var info))
                {
                    // Map info was found in cache. No further data retrieval nescessary.
                    return info;
                }
                else
                {
                    var infoFilePath = _fileSystem.Path.Combine(normalizedMapDirectory, "Info.dat");
                    var mapInfoText = await _fileSystem.File.ReadAllTextAsync(infoFilePath);

                    info = JsonSerializer.Deserialize<MapInfo>(mapInfoText);
                }

                info.Hash = mapHash;
                info.DirectoryPath = normalizedMapDirectory;

                var directoryName = _fileSystem.Path.GetFileName(_fileSystem.Path.GetDirectoryName(info.DirectoryPath + "/"));

                info.Id = _mapIdRegex.Match(directoryName)?.Value;

                if (string.IsNullOrEmpty(info.Id))
                    return null;

                var mapDirectoryInfo = _fileSystem.DirectoryInfo.New(normalizedMapDirectory);

                info.AddedDateTime = mapDirectoryInfo.CreationTime;

                FillSongInfo(info, songDurationCache);

                return info;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Failed to load map info for {mapDirectory}");

                return null;
            }
        }

        /// <summary>
        /// Fills additional song info of the given map that is not found within the Info.dat file of the map.
        /// </summary>
        private void FillSongInfo(MapInfo info, Dictionary<string, SongDuration> songDurationCache)
        {
            try
            {
                var relativeMapDirectory = GetRelativeMapPath(info.DirectoryPath);

                SongDuration? songDuration;

                // Check if song duration is found in SongCore cache (from relative map directory)
                if (songDurationCache.TryGetValue(relativeMapDirectory, out songDuration))
                {
                    info.SongDuration = TimeSpan.FromSeconds(songDuration.DurationInSeconds);
                }
                // Check if song duration is found in SongCore cache (from absolute map directory (legacy))
                else if (songDurationCache.TryGetValue(info.DirectoryPath, out songDuration))
                {
                    info.SongDuration = TimeSpan.FromSeconds(songDuration.DurationInSeconds);
                }
                else
                {
                    var audioFilePath = _fileSystem.Path.Combine(info.DirectoryPath, info.SongFileName);

                    if (!_fileSystem.File.Exists(audioFilePath))
                        return;

                    using (var audioFile = new VorbisReader(audioFilePath))
                    {
                        info.SongDuration = audioFile.TotalTime;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Failed to load additional song info for {info.DirectoryPath}");
            }
        }

        public Image GetMapCoverImage(string mapId)
        {
            var imageFilePath = GetMapCoverImageFilePath(mapId);

            return Image.FromFile(imageFilePath);
        }

        public Stream GetMapCoverImageStream(string mapId)
        {
            var imageFilePath = GetMapCoverImageFilePath(mapId);

            return File.OpenRead(imageFilePath);
        }

        public string GetMapCoverImageFilePath(string mapId)
        {
            var mapInfo = _mapInfo.Value[mapId];

            return _fileSystem.Path.Combine(mapInfo.DirectoryPath, mapInfo.CoverImageFilename);
        }

        public string GetMapSongPath(string mapId)
        {
            var mapInfo = _mapInfo.Value[mapId];

            return _fileSystem.Path.Combine(mapInfo.DirectoryPath, mapInfo.SongFileName);
        }

        public void SetInitialMapLoad(bool initialMapLoad)
        {
            _initialMapLoad.OnNext(initialMapLoad);
        }

        /// <summary>
        /// Returns the MapInfo from the cache.
        /// </summary>
        private async Task<IEnumerable<MapInfo>> GetMapInfoCache()
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetService<IDataStore>();

            return await dataStore.Set<MapInfo>().ToListAsync();
        }

        /// <summary>
        /// Writes the given MapInfo's into the cache db.
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

        private async Task RemoveMapsFromCache(IEnumerable<string> mapHashes)
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetService<IDataStore>();

            await dataStore.Set<MapInfo>()
                .Where(m => mapHashes.Contains(m.Hash))
                .ExecuteDeleteAsync();
        }

        public async Task ClearMapCache()
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetService<IDataStore>();

            await dataStore.Set<MapInfo>()
                .ExecuteDeleteAsync();
        }

        public async Task DeleteMap(string mapHash) => await DeleteMaps(new[] { mapHash });

        public async Task DeleteMaps(IEnumerable<string> mapHashes)
        {
            foreach (var mapHash in mapHashes)
            {
                if (!_mapInfo.Value.TryGetValue(mapHash, out var mapInfo))
                    continue;

                if (_fileSystem.Directory.Exists(mapInfo.DirectoryPath))
                {
                    var fileCount = _fileSystem.Directory
                        .EnumerateFiles(mapInfo.DirectoryPath, "*", SearchOption.AllDirectories)
                        .Count();

                    if (fileCount >= 50)
                        throw new Exception("Map directory contains more than 50 files. Fail safe measure to prevent accidental deletion of other directories.");

                    _fileSystem.Directory.Delete(mapInfo.DirectoryPath, true);
                }

                _mapInfo.Value.Remove(mapHash);
            }

            await RemoveMapsFromCache(mapHashes);

            _mapInfo.OnNext(_mapInfo.Value);
        }

        public bool MapIsLoaded(string mapHash) => _mapInfo.Value.ContainsKey(mapHash);

        /// <summary>
        /// Cleans the large object heap to remove playlist/map cover image data that is otherwise seldom cleaned by the garbage collector.
        /// </summary>
        public static void CleanLargeObjectHeap()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }
    }
}
