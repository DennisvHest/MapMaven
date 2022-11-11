using BeatSaberTools.Core.ApiClients;
using BeatSaberTools.Core.Models.Data.ScoreSaber;
using BeatSaberTools.Core.Services;
using BeatSaberTools.Core.Utilities.Scoresaber;
using BeatSaberTools.Models.Data;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly ScoreSaberService _scoreSaberService;

        private readonly BehaviorSubject<string?> _selectedSongAuthorName = new BehaviorSubject<string?>(null);

        public IObservable<IEnumerable<Map>> Maps { get; private set; }
        public IObservable<IEnumerable<Map>> CompleteMapData { get; private set; }
        public IObservable<Dictionary<string, Map>> MapsByHash { get; private set; }

        public IObservable<string?> SelectedSongAuthorName => _selectedSongAuthorName;


        public MapService(BeatSaberDataService beatSaberDataService, ScoreSaberService scoreSaberService)
        {
            _beatSaberDataService = beatSaberDataService;
            _scoreSaberService = scoreSaberService;

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
                map.ScoreEstimate = scoreEstimate.FirstOrDefault();

                return map;
            });
        }

        public void SelectSongAuthor(string name)
        {
            _selectedSongAuthorName.OnNext(name);
        }
    }
}
