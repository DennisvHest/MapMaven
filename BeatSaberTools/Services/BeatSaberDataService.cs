﻿using BeatSaberTools.Models.Data;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.RegularExpressions;
using NVorbis;
using BeatSaberTools.Models.Data.Playlists;
using BeatSaber.SongHashing;
using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics;
using Image = System.Drawing.Image;

namespace BeatSaberTools.Services
{
    public class BeatSaberDataService
    {
        private const string BeatSaberInstallLocation = @"E:/Games/SteamLibrary/steamapps/common/Beat Saber";
        private const string MapsLocation = $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";
        private const string PlaylistsLocation = $"{BeatSaberInstallLocation}/Playlists";
        private const string UserDataLocation = $"{BeatSaberInstallLocation}/UserData";
        private string MapInfoCachePath = Path.Combine(FileSystem.AppDataDirectory, "map-info.json");

        private readonly IBeatmapHasher _beatmapHasher;

        private readonly Regex _mapIdRegex = new Regex(@"^[0-9A-Fa-f]+");

        private readonly BehaviorSubject<Dictionary<string, MapInfo>> _mapInfo = new(new Dictionary<string, MapInfo>());
        private readonly BehaviorSubject<bool> _loadingMapInfo = new(false);

        private readonly BehaviorSubject<IEnumerable<PlaylistInfo>> _playlistInfo = new(Array.Empty<PlaylistInfo>());
        private readonly BehaviorSubject<bool> _loadingPlaylistInfo = new(false);

        public IObservable<IEnumerable<MapInfo>> MapInfo => _mapInfo.Select(x => x.Values);
        public IObservable<bool> LoadingMapInfo => _loadingMapInfo;

        public IObservable<IEnumerable<PlaylistInfo>> PlaylistInfo => _playlistInfo;
        public IObservable<bool> LoadingPlaylistInfo => _loadingPlaylistInfo;

        public BeatSaberDataService(IBeatmapHasher beatmapHasher)
        {
            _beatmapHasher = beatmapHasher;
        }

        public async Task LoadAllMapInfo()
        {
            _loadingMapInfo.OnNext(true);

            try
            {
                var songHashData = await GetSongHashData();
                var mapInfoCache = await GetMapInfoCache();

                var fileReadTasks = Directory.EnumerateDirectories(MapsLocation)
                    //.Take(10)
                    .Select(mapDirectory => GetMapInfo(mapDirectory, songHashData, mapInfoCache));

                IEnumerable<MapInfo> mapInfo = await Task.WhenAll(fileReadTasks);

                var mapInfoDictionary = mapInfo
                    .Where(i => i != null)
                    .GroupBy(i => i.Id)
                    .Select(g => g.First())
                    .ToDictionary(i => i.Id);

                await CacheMapInfo(mapInfoDictionary.Select(i => i.Value));

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
                var fileReadTasks = Directory.EnumerateFiles(PlaylistsLocation, "*.bplist")
                    .Select(async playlistFilePath =>
                    {
                        var playlistInfoText = await File.ReadAllTextAsync(playlistFilePath);

                        return JsonSerializer.Deserialize<PlaylistInfo>(playlistInfoText, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    });

                IEnumerable<PlaylistInfo> playlistInfo = await Task.WhenAll(fileReadTasks);

                _playlistInfo.OnNext(playlistInfo);
            }
            finally
            {
                _loadingPlaylistInfo.OnNext(false);
            }
        }

        private async Task<Dictionary<string, SongHash>> GetSongHashData()
        {
            var songHashFilePath = Path.Combine(UserDataLocation, "SongCore", "SongHashData.dat");
            var songHashJson = await File.ReadAllTextAsync(songHashFilePath);

            var songHashData = JsonSerializer.Deserialize<Dictionary<string, SongHash>>(songHashJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return songHashData.ToDictionary(x => x.Key.NormalizePath(), x => x.Value);
        }

        private async Task<MapInfo> GetMapInfo(string mapDirectory, Dictionary<string, SongHash> songHashData, Dictionary<string, MapInfo> mapInfoCache)
        {
            var infoFilePath = Path.Combine(mapDirectory, "Info.dat");
            var mapInfoText = await File.ReadAllTextAsync(infoFilePath);

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
                return info;
            }
            else
            {
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

        private async Task<Dictionary<string, MapInfo>> GetMapInfoCache()
        {
            Dictionary<string, MapInfo> mapInfoCache = new Dictionary<string, MapInfo>();

            if (File.Exists(MapInfoCachePath))
            {
                using (var mapInfoCacheStream = File.OpenRead(MapInfoCachePath))
                {
                    mapInfoCache = await JsonSerializer.DeserializeAsync<Dictionary<string, MapInfo>>(mapInfoCacheStream);
                }
            }

            return mapInfoCache;
        }

        private async Task CacheMapInfo(IEnumerable<MapInfo> mapInfo)
        {
            var mapInfoByHash = mapInfo
                .GroupBy(i => i.Hash)
                .Select(x => x.First())
                .ToDictionary(i => i.Hash);

            string mapInfoCacheJson = JsonSerializer.Serialize(mapInfoByHash);

            await File.WriteAllTextAsync(MapInfoCachePath, mapInfoCacheJson);
        }
    }
}
