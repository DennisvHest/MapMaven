using BeatSaberTools.Core.Services;
using System.Reactive.Linq;

namespace BeatSaberTools.Worker.Services
{
    public class BeatSaberWorkerFileService : BeatSaverFileServiceBase
    {
        public override string BeatSaberInstallLocation => @"F:/SteamLibrary/steamapps/common/Beat Saber";

        public override string MapInfoCachePath => null!;
        public override string HiddenMapConfigPath => null!;

        public override IObservable<string> BeatSaberInstallLocationObservable => Observable.Return(BeatSaberInstallLocation);


        public override void SetBeatSaberInstallLocation(string path) { }
    }
}
