using BeatSaberTools.Core.Services;
using System.Reactive.Linq;

namespace BeatSaberTools.Worker.Services
{
    public class BeatSaberWorkerFileService : IBeatSaverFileService
    {
        public string BeatSaberInstallLocation => @"F:/SteamLibrary/steamapps/common/Beat Saber";

        public string MapInfoCachePath => null!;
        public string HiddenMapConfigPath => null!;

        public IObservable<string> BeatSaberInstallLocationObservable => Observable.Return(BeatSaberInstallLocation);


        public void SetBeatSaberInstallLocation(string path) { }
    }
}
