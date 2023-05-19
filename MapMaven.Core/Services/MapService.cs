using MapMaven.Core.ApiClients;
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

namespace MapMaven.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly ScoreSaberService _scoreSaberService;
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
            BeatSaberDataService beatSaberDataService,
            ScoreSaberService scoreSaberService,
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
                _scoreSaberService.PlayerScores.StartWith(Enumerable.Empty<PlayerScore>()),
                _scoreSaberService.RankedMaps,
                _scoreSaberService.ScoreEstimates.StartWith(Enumerable.Empty<ScoreEstimate>()),
                CombineMapData);

            var hiddenMaps = Observable.CombineLatest(_hiddenMaps, _scoreSaberService.PlayerProfile, (hiddenMaps, player) =>
            {
                if (hiddenMaps == null || player == null)
                    return Enumerable.Empty<HiddenMap>();

                return hiddenMaps.Where(m => m.PlayerId == player.Id);
            });

            RankedMaps = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.RankedMaps,
                _scoreSaberService.RankedMapScoreEstimates,
                _scoreSaberService.PlayerScores,
                hiddenMaps,
                CombineRankedMapData);

            CompleteMapData = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.PlayerScores,
                _scoreSaberService.RankedMaps,
                _scoreSaberService.ScoreEstimates,
                CombineMapData);

            CompleteRankedMapData = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.RankedMaps,
                _scoreSaberService.RankedMapScoreEstimates.Where(x => x.Any()),
                _scoreSaberService.PlayerScores.Where(x => x.Any()),
                hiddenMaps,
                CombineRankedMapData);
        }

        private IEnumerable<Map> CombineMapData(IEnumerable<MapInfo> maps, IEnumerable<PlayerScore> playerScores, IEnumerable<RankedMap> rankedMaps, IEnumerable<ScoreEstimate> scoreEstimates)
        {
            return maps.GroupJoin(playerScores, mapInfo => mapInfo.Hash, score => score.Leaderboard.SongHash, (mapInfo, scores) =>
            {
                var map = mapInfo.ToMap();

                map.PlayerScore = scores.MaxBy(s => s.Score.Pp);

                return map;
            }).GroupJoin(rankedMaps, map => map.Hash, rankedMap => rankedMap.Id, (map, rankedMap) =>
            {
                map.RankedMap = rankedMap.FirstOrDefault();

                return map;
            }).GroupJoin(scoreEstimates, map => map.Hash, scoreEstimate => scoreEstimate.MapId, (map, scoreEstimate) =>
            {
                map.ScoreEstimates = scoreEstimate;

                return map;
            }).ToList();
        }

        private IEnumerable<Map> CombineRankedMapData(
            IEnumerable<MapInfo> maps,
            IEnumerable<RankedMap> rankedMaps,
            IEnumerable<ScoreEstimate> scoreEstimates,
            IEnumerable<PlayerScore> playerScores,
            IEnumerable<HiddenMap> hiddenMaps)
        {
            return rankedMaps
                .GroupJoin(scoreEstimates, map => map.Id + map.Difficulty, scoreEstimate => scoreEstimate.MapId + scoreEstimate.Difficulty, (rankedMap, scoreEstimates) =>
                {
                    var map = rankedMap.ToMap();

                    map.ScoreEstimates = scoreEstimates;
                    map.RankedMap = rankedMap;

                    return map;
                }).GroupJoin(playerScores, map => map.RankedMap.Id + map.RankedMap.Difficulty, score => score.Leaderboard.SongHash + score.Leaderboard.Difficulty.DifficultyName, (map, scores) =>
                {
                    map.PlayerScore = scores.MaxBy(s => s.Score.Pp);

                    return map;
                }).GroupJoin(hiddenMaps, map => map.Hash + map.RankedMap.Difficulty, hiddenMap => hiddenMap.Hash + hiddenMap.Difficulty, (map, hiddenMap) =>
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

        public void SelectMaps(IEnumerable<Map> selectedMaps)
        {
            _selectedMaps.OnNext(selectedMaps.ToHashSet());
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
