using BeatSaberTools.Core.Services;
using System.Reactive.Subjects;

namespace BeatSaberTools.Services
{
    public class BeatSaberToolFileService : BeatSaverFileServiceBase
    {
        private const string _preferencesSharedName = "BSTools";

        private const string _installLocationPreferencesKey = "beat_saber_install_location";


        public override string BeatSaberInstallLocation => _beatSaberInstallLocation.Value;
        public override string MapInfoCachePath => Path.Combine(FileSystem.AppDataDirectory, "map-info.json");
        public override string HiddenMapConfigPath => Path.Combine(FileSystem.AppDataDirectory, "hidden-map-config.json");


        private BehaviorSubject<string> _beatSaberInstallLocation = new(Preferences.Get(_installLocationPreferencesKey, null, _preferencesSharedName));

        public override IObservable<string> BeatSaberInstallLocationObservable => _beatSaberInstallLocation;

        public override void SetBeatSaberInstallLocation(string path)
        {
            path = path.Replace('\\', '/');

            Preferences.Set(_installLocationPreferencesKey, path, _preferencesSharedName);
            _beatSaberInstallLocation.OnNext(path);
        }
    }
}
