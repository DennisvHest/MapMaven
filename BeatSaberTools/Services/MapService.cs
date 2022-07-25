using BeatSaberTools.Models;
using System.Reactive.Linq;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;

        public IObservable<IEnumerable<Map>> Maps { get; private set; }
        public IObservable<Dictionary<string, Map>> MapsByHash { get; private set; }

        public MapService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;

            MapsByHash = _beatSaberDataService.MapInfoByHash
                .Select(x => x
                    .Select(i => KeyValuePair.Create(i.Key, i.Value.ToMap()))
                    .ToDictionary(x => x.Key, x => x.Value
                )
            );
            Maps = _beatSaberDataService.MapInfo.Select(x => x.Select(i => i.ToMap()));
        }
    }
}
