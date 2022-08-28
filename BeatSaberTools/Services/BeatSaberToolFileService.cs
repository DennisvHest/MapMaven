using BeatSaberTools.Core.Services;

namespace BeatSaberTools.Services
{
    public class BeatSaberToolFileService : IBeatSaverFileService
    {
        public string BeatSaberInstallLocation => @"F:/SteamLibrary/steamapps/common/Beat Saber";

        public string MapInfoCachePath => Path.Combine(FileSystem.AppDataDirectory, "map-info.json");
    }
}
