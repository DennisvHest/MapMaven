using BeatSaberTools.Core.Services;

namespace BeatSaberTools.Worker.Services
{
    public class BeatSaberWorkerFileService : IBeatSaverFileService
    {
        public string BeatSaberInstallLocation => @"F:/SteamLibrary/steamapps/common/Beat Saber";

        public string MapInfoCachePath => null!;

        public void SetBeatSaberInstallLocation(string path) { }
    }
}
