using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Models;
using BeatSaberTools.Core.Models.Data.ScoreSaber;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Core.Utilities.BeatSaver;
using BeatSaberTools.Core.Utilities.Scoresaber;
using BeatSaberTools.Models.Data;
using BeatSaverSharp;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly ScoreSaberService _scoreSaberService;
        private readonly BeatSaverFileServiceBase _fileService;

        private readonly BeatSaver _beatSaver;

        private readonly BehaviorSubject<List<MapFilter>> _mapFilters = new(new List<MapFilter>());

        private readonly BehaviorSubject<HashSet<Map>> _selectedMaps = new(Enumerable.Empty<Map>().ToHashSet());

        private readonly BehaviorSubject<HiddenMapConfiguration> _hiddenMapConfig = new(new());

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
            BeatSaverFileServiceBase fileService)
        {
            _beatSaberDataService = beatSaberDataService;
            _scoreSaberService = scoreSaberService;
            _fileService = fileService;

            _beatSaver = beatSaver;

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

            var hiddenMaps = Observable.CombineLatest(_hiddenMapConfig, _scoreSaberService.PlayerProfile, (hiddenMapConfig, player) =>
            {
                if (hiddenMapConfig == null || player == null)
                    return new HashSet<HiddenMap>();

                return hiddenMapConfig?.Items
                    .FirstOrDefault(i => i.PlayerId == player.Id)
                    ?.Maps ?? new HashSet<HiddenMap>();
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

                map.PlayerScore = scores.FirstOrDefault();

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
            HashSet<HiddenMap> hiddenMaps)
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
                    map.PlayerScore = scores.FirstOrDefault();

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

        public async Task DownloadMap(Map map, bool force = false, IProgress<double>? progress = null)
        {
            if (!force && MapIsInstalled(map))
            {
                progress?.Report(1);
                return;
            }

            var beatMap = await _beatSaver.BeatmapByHash(map.Hash);

            await MapInstaller.InstallMap(beatMap, _fileService.MapsLocation, progress);

            await _beatSaberDataService.LoadMapInfo(beatMap.ID);
        }

        public bool MapIsInstalled(Map map)
        {
            return MapInstaller.MapIsInstalled(map, _fileService.MapsLocation);
        }

        public async Task LoadHiddenMapConfig()
        {
            if (!File.Exists(_fileService.HiddenMapConfigPath))
                return;

            HiddenMapConfiguration hiddenMapConfig;

            using (var hiddenMapConfigStream = File.OpenRead(_fileService.HiddenMapConfigPath))
            {
                hiddenMapConfig = await JsonSerializer.DeserializeAsync<HiddenMapConfiguration>(hiddenMapConfigStream);
            }

            _hiddenMapConfig.OnNext(hiddenMapConfig);
        }

        public async Task HideUnhideMap(Map map)
        {
            var hiddenMapConfig = _hiddenMapConfig.Value;
            var playerId = _scoreSaberService.PlayerId;

            var configItem = hiddenMapConfig.Items.FirstOrDefault(i => i.PlayerId == playerId);

            if (configItem == null)
            {
                configItem = new HiddenMapConfigurationItem
                {
                    PlayerId = playerId
                };

                hiddenMapConfig.Items.Add(configItem);
            }

            var hiddenMap = new HiddenMap(map);

            if (map.Hidden)
            {
                configItem.Maps.Remove(hiddenMap);
            }
            else
            {
                configItem.Maps.Add(hiddenMap);
            }

            await SaveHiddenMapConfig(hiddenMapConfig);

            _hiddenMapConfig.OnNext(_hiddenMapConfig.Value);
        }

        private async Task SaveHiddenMapConfig(HiddenMapConfiguration hiddenMapConfig)
        {
            string hiddenMapConfigJson = JsonSerializer.Serialize(hiddenMapConfig);

            await File.WriteAllTextAsync(_fileService.HiddenMapConfigPath, hiddenMapConfigJson);
        }
    }
}
