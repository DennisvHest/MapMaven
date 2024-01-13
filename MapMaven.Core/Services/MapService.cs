using MapMaven.Core.Models;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Services;
using MapMaven.Core.Utilities.BeatSaver;
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
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Models.Data.Leaderboards;

namespace MapMaven.Services
{
    public class MapService : IMapService
    {
        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly ILeaderboardService _leaderBoardService;
        private readonly BeatSaberFileService _fileService;
        private readonly SongPlayerService _songPlayerService;
        private readonly ILeaderboardDataService _leaderboardDataService;

        private readonly BeatSaver _beatSaver;

        private readonly IServiceProvider _serviceProvider;

        private readonly BehaviorSubject<List<MapFilter>> _mapFilters = new(new List<MapFilter>());

        private readonly BehaviorSubject<HashSet<Map>> _selectedMaps = new(Enumerable.Empty<Map>().ToHashSet());
        private readonly BehaviorSubject<bool> _selectable = new(false);

        private readonly BehaviorSubject<IEnumerable<HiddenMap>> _hiddenMaps = new(Enumerable.Empty<HiddenMap>());

        public IObservable<IEnumerable<Map>> Maps { get; private set; }
        public IObservable<IEnumerable<Map>> RankedMaps { get; private set; }
        public IObservable<IEnumerable<Map>> CompleteMapData { get; private set; }
        public IObservable<IEnumerable<Map>> CompleteRankedMapData { get; private set; }
        public IObservable<Dictionary<string, Map>> MapsByHash { get; private set; }
        public IObservable<IEnumerable<HiddenMap>> HiddenMaps { get; private set; }

        public IObservable<HashSet<Map>> SelectedMaps => _selectedMaps;
        public IObservable<bool> Selectable => _selectable;

        public IObservable<IEnumerable<MapFilter>> MapFilters => _mapFilters;


        public MapService(
            IBeatSaberDataService beatSaberDataService,
            ILeaderboardService leaderBoardService,
            BeatSaver beatSaver,
            BeatSaberFileService fileService,
            IServiceProvider serviceProvider,
            SongPlayerService songPlayerService,
            ILeaderboardDataService leaderboardDataService)
        {
            _beatSaberDataService = beatSaberDataService;
            _leaderBoardService = leaderBoardService;
            _songPlayerService = songPlayerService;
            _fileService = fileService;
            _beatSaver = beatSaver;
            _serviceProvider = serviceProvider;
            _leaderboardDataService = leaderboardDataService;

            MapsByHash = _beatSaberDataService.MapInfoByHash
                .Select(x => x
                    .Select(i => KeyValuePair.Create(i.Key, i.Value.ToMap()))
                    .ToDictionary(x => x.Key, x => x.Value
                )
            );

            Maps = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _leaderBoardService.RankedMaps.StartWith(new Dictionary<string, RankedMapInfoItem>()),
                _leaderBoardService.PlayerScores.StartWith(Enumerable.Empty<PlayerScore>()),
                _leaderBoardService.RankedMapScoreEstimates.StartWith(Enumerable.Empty<ScoreEstimate>()),
                CombineMapData);

            HiddenMaps = Observable.CombineLatest(_hiddenMaps, _leaderBoardService.PlayerProfile, (hiddenMaps, player) =>
            {
                if (hiddenMaps == null || player == null)
                    return Enumerable.Empty<HiddenMap>();

                return hiddenMaps.Where(m => m.PlayerId == player.Id);
            });

            RankedMaps = Observable.CombineLatest(
                _leaderBoardService.RankedMaps,
                _leaderBoardService.RankedMapScoreEstimates,
                _leaderBoardService.PlayerScores,
                HiddenMaps,
                CombineRankedMapData);

            CompleteMapData = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _leaderBoardService.RankedMaps,
                _leaderBoardService.PlayerScores,
                _leaderBoardService.RankedMapScoreEstimates,
                CombineMapData);

            CompleteRankedMapData = Observable.CombineLatest(
                _leaderBoardService.RankedMaps,
                _leaderBoardService.RankedMapScoreEstimates,
                _leaderBoardService.PlayerScores,
                HiddenMaps,
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
                map.AllPlayerScores = scores.OrderByDescending(s => DifficultyUtils.GetOrder(s.Leaderboard.Difficulty)).ToList();
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
                }).GroupJoin(playerScores, map => map.Hash + map.Difficulty.Difficulty, score => score.Leaderboard.SongHash + score.Leaderboard.Difficulty, (map, scores) =>
                {
                    map.HighestPlayerScore = scores.MaxBy(s => s.Score.Pp);

                    return map;
                }).GroupJoin(hiddenMaps, map => map.Hash + map.Difficulty.Difficulty, hiddenMap => hiddenMap.Hash + hiddenMap.Difficulty, (map, hiddenMap) =>
                {
                    map.Hidden = hiddenMap.Any();

                    return map;
                }).ToList();
        }

        public async Task<IEnumerable<Map>> GetCompleteRankedMapDataForLeaderboardProvider(LeaderboardProvider leaderboardProvider)
        {
            var leaderBoardService = _leaderBoardService.LeaderboardProviders[leaderboardProvider];
            var scoreEstimationService = _leaderBoardService.ScoreEstimationServices[leaderboardProvider];

            var combinedData = Observable.CombineLatest(
                leaderBoardService.RankedMaps,
                scoreEstimationService.RankedMapScoreEstimates,
                leaderBoardService.PlayerScores,
                HiddenMaps,
                CombineRankedMapData);

            return await combinedData.FirstAsync();
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

        public void CancelSelection() => SetSelectable(false);

        public void SelectMaps(IEnumerable<Map> selectedMaps)
        {
            _selectedMaps.OnNext(selectedMaps.ToHashSet());
        }

        public void SetSelectable(bool selectable)
        {
            _selectable.OnNext(selectable);

            if (!selectable)
                ClearSelectedMaps();
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

        public async Task HideUnhideMap(Map map) => await HideUnhideMap(new[] { map }, !map.Hidden);

        public async Task HideUnhideMap(IEnumerable<Map> maps, bool hide)
        {
            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetRequiredService<IDataStore>();

            var player = await AddPlayerIfNotExists(dataStore);

            foreach (var map in maps)
            {
                var hiddenMap = new HiddenMap(map);

                var existingHiddenMap = player.HiddenMaps.FirstOrDefault(m =>
                    m.Hash == hiddenMap.Hash
                    && m.Difficulty == hiddenMap.Difficulty
                );

                if (hide)
                {
                    if (existingHiddenMap == null)
                    {
                        hiddenMap.PlayerId = player.Id;
                        dataStore.Set<HiddenMap>().Add(hiddenMap);
                    }
                }
                else
                {
                    if (existingHiddenMap != null)
                        dataStore.Set<HiddenMap>().Remove(existingHiddenMap);
                }
            }

            await dataStore.SaveChangesAsync();

            await LoadHiddenMaps();
        }

        public async Task DeleteMap(string mapHash) => await DeleteMaps(new string[] { mapHash });

        public async Task DeleteMaps(IEnumerable<string> mapHashes)
        {
            foreach (var mapHash in mapHashes)
            {
                _songPlayerService.StopIfPlaying(mapHash);
            }

            await _beatSaberDataService.DeleteMaps(mapHashes);
        }

        private async Task<Player> AddPlayerIfNotExists(IDataStore dataStore)
        {
            var playerId = _leaderBoardService.PlayerId;

            var player = await dataStore.Set<Player>()
                .Include(p => p.HiddenMaps)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                player = new Player { Id = playerId };

                dataStore.Set<Player>().Add(player);
            }

            return player;
        }

        public async Task RefreshDataAsync(bool reloadMapAndLeaderboardInfo = false, bool forceReloadCachedData = false)
        {
            var tasks = new List<Task>()
            {
                _leaderboardDataService.LoadLeaderboardDataAsync(),
                _beatSaberDataService.LoadAllPlaylists(),
                LoadHiddenMaps()
            };

            if (reloadMapAndLeaderboardInfo)
            {
                tasks.Add(_beatSaberDataService.LoadAllMapInfo());
                _leaderBoardService.RefreshPlayerData();
            }

            if (forceReloadCachedData)
            {
                _leaderBoardService.ReloadRankedMaps();
            }

            await Task.WhenAll(tasks);
        }
    }
}
