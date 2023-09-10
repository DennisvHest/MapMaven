﻿using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Core.Services;
using MapMaven.Core.Utilities.BeatSaver;
using MapMaven.Core.Utilities.Scoresaber;
using MapMaven.Models.Data;
using BeatSaverSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = MapMaven.Models.Map;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities;
using MapMaven.Core.Models.Data.RankedMaps;

namespace MapMaven.Services
{
    public class MapService : IMapService
    {
        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly IScoreSaberService _scoreSaberService;
        private readonly BeatSaberFileService _fileService;

        private readonly BeatSaver _beatSaver;

        private readonly IServiceProvider _serviceProvider;

        private readonly BehaviorSubject<List<MapFilter>> _mapFilters = new(new List<MapFilter>());

        private readonly BehaviorSubject<HashSet<Map>> _selectedMaps = new(Enumerable.Empty<Map>().ToHashSet());

        private readonly BehaviorSubject<IEnumerable<HiddenMap>> _hiddenMaps = new(Enumerable.Empty<HiddenMap>());

        public IObservable<IEnumerable<Map>> Maps { get; private set; }
        public IObservable<IEnumerable<Map>> RankedMaps { get; private set; }
        public IObservable<IEnumerable<Map>> CompleteMapData { get; private set; }
        public IObservable<IEnumerable<Map>> CompleteRankedMapData { get; private set; }
        public IObservable<Dictionary<string, Map>> MapsByHash { get; private set; }

        public IObservable<HashSet<Map>> SelectedMaps => _selectedMaps;

        public IObservable<IEnumerable<MapFilter>> MapFilters => _mapFilters;


        public MapService(
            IBeatSaberDataService beatSaberDataService,
            IScoreSaberService scoreSaberService,
            BeatSaver beatSaver,
            BeatSaberFileService fileService,
            IServiceProvider serviceProvider)
        {
            _beatSaberDataService = beatSaberDataService;
            _scoreSaberService = scoreSaberService;
            _fileService = fileService;
            _beatSaver = beatSaver;
            _serviceProvider = serviceProvider;

            MapsByHash = _beatSaberDataService.MapInfoByHash
                .Select(x => x
                    .Select(i => KeyValuePair.Create(i.Key, i.Value.ToMap()))
                    .ToDictionary(x => x.Key, x => x.Value
                )
            );

            Maps = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.RankedMaps.StartWith(new Dictionary<string, RankedMapInfoItem>()),
                _scoreSaberService.PlayerScores.StartWith(Enumerable.Empty<PlayerScore>()),
                _scoreSaberService.RankedMapScoreEstimates.StartWith(Enumerable.Empty<ScoreEstimate>()),
                CombineMapData);

            var hiddenMaps = Observable.CombineLatest(_hiddenMaps, _scoreSaberService.PlayerProfile, (hiddenMaps, player) =>
            {
                if (hiddenMaps == null || player == null)
                    return Enumerable.Empty<HiddenMap>();

                return hiddenMaps.Where(m => m.PlayerId == player.Id);
            });

            RankedMaps = Observable.CombineLatest(
                _scoreSaberService.RankedMaps,
                _scoreSaberService.RankedMapScoreEstimates,
                _scoreSaberService.PlayerScores,
                hiddenMaps,
                CombineRankedMapData);

            CompleteMapData = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.RankedMaps,
                _scoreSaberService.PlayerScores,
                _scoreSaberService.RankedMapScoreEstimates,
                CombineMapData);

            CompleteRankedMapData = Observable.CombineLatest(
                _scoreSaberService.RankedMaps,
                _scoreSaberService.RankedMapScoreEstimates,
                _scoreSaberService.PlayerScores,
                hiddenMaps,
                CombineRankedMapData);
        }

        private IEnumerable<Map> CombineMapData(IEnumerable<MapInfo> maps, Dictionary<string, RankedMapInfoItem> rankedMaps, IEnumerable<PlayerScore> playerScores, IEnumerable<ScoreEstimate> scoreEstimates)
        {
            return maps.GroupJoin(rankedMaps, mapInfo => mapInfo.Hash, rankedMap => rankedMap.Key, (mapInfo, rankedMaps) =>
            {
                var map = mapInfo.ToMap();

                map.SetRankedMapDetails(rankedMaps.FirstOrDefault().Value);

                return map;
            }).GroupJoin(playerScores, mapInfo => mapInfo.Hash, score => score.Leaderboard.SongHash, (map, scores) =>
            {
                map.AllPlayerScores = scores.OrderByDescending(s => s.Leaderboard.Difficulty.Difficulty1).ToList();
                map.HighestPlayerScore = scores.MaxBy(s => s.Score.Pp);

                return map;
            }).GroupJoin(scoreEstimates, map => map.Hash, scoreEstimate => scoreEstimate.MapHash, (map, scoreEstimate) =>
            {
                map.ScoreEstimates = scoreEstimate.OrderByDescending(s => DifficultyUtils.GetOrder(s.Difficulty));

                return map;
            }).ToList();
        }

        private IEnumerable<Map> CombineRankedMapData(
            Dictionary<string, RankedMapInfoItem> rankedMaps,
            IEnumerable<ScoreEstimate> scoreEstimates,
            IEnumerable<PlayerScore> playerScores,
            IEnumerable<HiddenMap> hiddenMaps)
        {
            return rankedMaps
                .SelectMany(x => x.Value.Difficulties.Select(d => (Difficulty: d, Map: x)))
                .GroupJoin(scoreEstimates, map => map.Map.Key + map.Difficulty.Difficulty, scoreEstimate => scoreEstimate.MapHash + scoreEstimate.Difficulty, (rankedMap, scoreEstimates) =>
                {
                    var map = rankedMap.Map.Value.ToMap();

                    map.ScoreEstimates = scoreEstimates.OrderByDescending(s => DifficultyUtils.GetOrder(s.Difficulty));
                    map.Difficulty = rankedMap.Difficulty;

                    return map;
                }).GroupJoin(playerScores, map => map.Hash + map.Difficulty.Difficulty, score => score.Leaderboard.SongHash + score.Leaderboard.Difficulty.DifficultyName, (map, scores) =>
                {
                    map.HighestPlayerScore = scores.MaxBy(s => s.Score.Pp);

                    return map;
                }).GroupJoin(hiddenMaps, map => map.Hash + map.Difficulty.Difficulty, hiddenMap => hiddenMap.Hash + hiddenMap.Difficulty, (map, hiddenMap) =>
                {
                    map.Hidden = hiddenMap.Any();

                    return map;
                }).ToList();
        }

        public void AddMapFilter(MapFilter filter)
        {
            _mapFilters.Value.Add(filter);

            _mapFilters.OnNext(_mapFilters.Value);
        }

        public void RemoveMapFilter(MapFilter filter)
        {
            _mapFilters.Value.Remove(filter);

            _mapFilters.OnNext(_mapFilters.Value);
        }

        public void ClearMapFilters()
        {
            _mapFilters.Value.Clear();

            _mapFilters.OnNext(_mapFilters.Value);
        }

        public void SetSelectedMaps(HashSet<Map> selectedMaps) => _selectedMaps.OnNext(selectedMaps);

        public void ClearSelectedMaps() => _selectedMaps.OnNext(new HashSet<Map>());

        public void ResetSelectedMaps() => _selectedMaps.OnNext(_selectedMaps.Value);

        public void SelectMaps(IEnumerable<Map> selectedMaps)
        {
            _selectedMaps.OnNext(selectedMaps.ToHashSet());
        }

        public async Task<Map> GetMapDetails(Map map)
        {
            var beatMap = await _beatSaver.BeatmapByHash(map.Hash);

            if (beatMap != null)
                map.SetMapDetails(beatMap);

            return map;
        }

        public async Task DownloadMap(Map map, bool force = false, IProgress<double>? progress = null, bool loadMapInfo = true, CancellationToken cancellationToken = default)
        {
            if (!force && MapIsInstalled(map))
            {
                progress?.Report(1);
                return;
            }

            var beatMap = await _beatSaver.BeatmapByHash(map.Hash);

            await MapInstaller.InstallMap(beatMap, _fileService.MapsLocation, progress, cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (loadMapInfo)
                await _beatSaberDataService.LoadMapInfo(beatMap.ID);
        }

        public bool MapIsInstalled(Map map)
        {
            return MapInstaller.MapIsInstalled(map, _fileService.MapsLocation);
        }

        public async Task LoadHiddenMaps()
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetRequiredService<IDataStore>();

            var hiddenMaps = await dataStore.Set<HiddenMap>().ToListAsync();

            _hiddenMaps.OnNext(hiddenMaps);
        }

        public async Task HideUnhideMap(Map map)
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetRequiredService<IDataStore>();

            var playerId = _scoreSaberService.PlayerId;

            var player = await dataStore.Set<Core.Models.Data.Player>()
                .Include(p => p.HiddenMaps)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                player = new Core.Models.Data.Player { Id = playerId };

                dataStore.Set<Core.Models.Data.Player>().Add(player);
            }

            var hiddenMap = new HiddenMap(map);

            if (map.Hidden)
            {
                var mapToRemove = player.HiddenMaps.FirstOrDefault(m => m.Hash == map.Hash);

                dataStore.Set<HiddenMap>().Remove(mapToRemove);
            }
            else
            {
                hiddenMap.PlayerId = playerId;
                dataStore.Set<HiddenMap>().Add(hiddenMap);
            }

            await dataStore.SaveChangesAsync();

            await LoadHiddenMaps();
        }

        public async Task RefreshDataAsync(bool forceRefresh = false)
        {
            IEnumerable<Task> tasks = new List<Task>()
            {
                _scoreSaberService.LoadRankedMaps(),
                _beatSaberDataService.LoadAllPlaylists(),
                LoadHiddenMaps()
            };

            if (forceRefresh)
            {
                tasks = new Task[] { _beatSaberDataService.LoadAllMapInfo() }.Concat(tasks);
                _scoreSaberService.RefreshPlayerData();
            }

            await Task.WhenAll(tasks);
        }
    }
}
