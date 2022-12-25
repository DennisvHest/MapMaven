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
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly ScoreSaberService _scoreSaberService;
        private readonly IBeatSaverFileService _fileService;

        private readonly BeatSaver _beatSaver;

        private readonly BehaviorSubject<List<MapFilter>> _mapFilters = new(new List<MapFilter>());

        private readonly BehaviorSubject<HashSet<Map>> _selectedMaps = new(Enumerable.Empty<Map>().ToHashSet());

        public IObservable<IEnumerable<Map>> Maps { get; private set; }
        public IObservable<IEnumerable<Map>> RankedMaps { get; private set; }
        public IObservable<IEnumerable<Map>> CompleteMapData { get; private set; }
        public IObservable<Dictionary<string, Map>> MapsByHash { get; private set; }

        public IObservable<HashSet<Map>> SelectedMaps => _selectedMaps;

        public IObservable<IEnumerable<MapFilter>> MapFilters => _mapFilters;


        public MapService(
            BeatSaberDataService beatSaberDataService,
            ScoreSaberService scoreSaberService,
            BeatSaver beatSaver,
            IBeatSaverFileService fileService)
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

            RankedMaps = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.RankedMaps,
                _scoreSaberService.RankedMapScoreEstimates,
                _scoreSaberService.PlayerScores,
                CombineRankedMapData);

            CompleteMapData = Observable.CombineLatest(
                _beatSaberDataService.MapInfo,
                _scoreSaberService.PlayerScores,
                _scoreSaberService.RankedMaps,
                _scoreSaberService.ScoreEstimates,
                CombineMapData);
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
                map.ScoreEstimate = scoreEstimate;

                return map;
            }).ToList();
        }

        private IEnumerable<Map> CombineRankedMapData(IEnumerable<MapInfo> maps, IEnumerable<RankedMap> rankedMaps, IEnumerable<ScoreEstimate> scoreEstimates, IEnumerable<PlayerScore> playerScores)
        {
            return rankedMaps.GroupJoin(scoreEstimates, map => map.Id + map.Difficulty, scoreEstimate => scoreEstimate.MapId + scoreEstimate.Difficulty, (rankedMap, scoreEstimates) =>
            {
                var map = rankedMap.ToMap();

                map.ScoreEstimate = scoreEstimates;
                map.RankedMap = rankedMap;

                return map;
            }).GroupJoin(playerScores, map => map.RankedMap.Id + map.RankedMap.Difficulty, score => score.Leaderboard.SongHash + score.Leaderboard.Difficulty.DifficultyName, (map, scores) =>
            {
                map.PlayerScore = scores.FirstOrDefault();

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
    }
}
