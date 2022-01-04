using BeatSaberTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace BeatSaberTools.Services
{
    public class MapService
    {
        private readonly BeatSaberDataService _beatSaberDataService;

        public IObservable<IEnumerable<Map>> Maps { get; private set; }

        public MapService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;

            Maps = _beatSaberDataService.MapInfo.Select(x => x.Select(i => i.ToMap()));
        }
    }
}
