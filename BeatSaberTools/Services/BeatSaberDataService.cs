using BeatSaberTools.Models.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeatSaberTools.Services
{
    public class BeatSaberDataService
    {
        private const string BeatSaberInstallLocation = @"E:/Games/SteamLibrary/steamapps/common/Beat Saber";
        private const string MapsLocation = $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";

        public async Task<IEnumerable<MapInfo>> GetAllMapInfo()
        {
            var fileReadTasks = Directory.EnumerateDirectories(MapsLocation)
                .Take(5)
                .Select(async mapDirectory =>
                {
                    var mapInfoText = await File.ReadAllTextAsync($"{mapDirectory}/Info.dat");

                    return JsonSerializer.Deserialize<MapInfo>(mapInfoText);
                });

            return await Task.WhenAll(fileReadTasks);
        }
    }
}
