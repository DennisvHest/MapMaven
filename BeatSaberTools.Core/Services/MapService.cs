using BeatSaberTools.Core.Services;
using System.Reactive.Linq;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly ScoreSaberService _scoreSaberService;

        public IObservable<IEnumerable<Map>> Maps { get; private set; }
        public IObservable<Dictionary<string, Map>> MapsByHash { get; private set; }

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

            Maps = Observable.CombineLatest(_beatSaberDataService.MapInfo, _scoreSaberService.PlayerScores, (maps, playerScores) =>
            {
                return maps.GroupJoin(playerScores, mapInfo => mapInfo.Hash, score => score.Leaderboard.SongHash, (mapInfo, scores) =>
                {
                    var map = mapInfo.ToMap();

                    map.PlayerScore = scores.FirstOrDefault();

                    return map;
                });
            });
        }
    }
}
