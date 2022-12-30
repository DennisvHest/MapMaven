using BeatSaberTools.Core.Services;
using System.Reactive.Subjects;

namespace BeatSaberTools.Services
{
    public class BeatSaberToolFileService : IBeatSaverFileService
    {
        private const string _preferencesSharedName = "BSTools";

        private const string _installLocationPreferencesKey = "beat_saber_install_location";


        public string BeatSaberInstallLocation => _beatSaberInstallLocation.Value;
        public string MapInfoCachePath => Path.Combine(FileSystem.AppDataDirectory, "map-info.json");
        public string HiddenMapConfigPath => Path.Combine(FileSystem.AppDataDirectory, "hidden-map-config.json");


        private BehaviorSubject<string> _beatSaberInstallLocation = new(Preferences.Get(_installLocationPreferencesKey, null, _preferencesSharedName));

        public IObservable<string> BeatSaberInstallLocationObservable => _beatSaberInstallLocation;

        public void SetBeatSaberInstallLocation(string path)
        {
            path = path.Replace('\\', '/');

            Preferences.Set(_installLocationPreferencesKey, path, _preferencesSharedName);
            _beatSaberInstallLocation.OnNext(path);
        }
    }
}
