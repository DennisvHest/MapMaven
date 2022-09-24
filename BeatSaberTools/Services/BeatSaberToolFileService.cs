using BeatSaberTools.Core.Services;

namespace BeatSaberTools.Services
{
    public class BeatSaberToolFileService : IBeatSaverFileService
    {
        private const string _preferencesSharedName = "BSTools";

        private const string _installLocationPreferencesKey = "beat_saber_install_location";

        public string BeatSaberInstallLocation => Preferences.Get(_installLocationPreferencesKey, null, _preferencesSharedName);

        public string MapInfoCachePath => Path.Combine(FileSystem.AppDataDirectory, "map-info.json");

        public void SetBeatSaberInstallLocation(string path)
        {
            path = path.Replace('\\', '/');

            Preferences.Set(_installLocationPreferencesKey, path, _preferencesSharedName);
        }
    }
}
