using BeatSaberTools.Models.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeatSaberTools.Services
{
    public class BeatSaberDataService
    {
        private const string BeatSaberInstallLocation = @"E:/Games/SteamLibrary/steamapps/common/Beat Saber";
        private const string MapsLocation = $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";

        private readonly BehaviorSubject<IEnumerable<MapInfo>> _mapInfo = new(Array.Empty<MapInfo>());
        private readonly BehaviorSubject<bool> _loadingMapInfo = new(false);

        public IObservable<IEnumerable<MapInfo>> MapInfo => _mapInfo;
        public IObservable<bool> LoadingMapInfo => _loadingMapInfo;

        public async Task LoadAllMapInfo()
        {
            _loadingMapInfo.OnNext(true);

            try
            {
                var fileReadTasks = Directory.EnumerateDirectories(MapsLocation)
                .Select(async mapDirectory =>
                {
                    var mapInfoText = await File.ReadAllTextAsync($"{mapDirectory}/Info.dat");

                    return JsonSerializer.Deserialize<MapInfo>(mapInfoText);
                });

                var mapInfo = await Task.WhenAll(fileReadTasks);

                await Task.Delay(5000);

                _mapInfo.OnNext(mapInfo);
            }
            finally
            {
                _loadingMapInfo.OnNext(false);
            }
        }
    }
}
