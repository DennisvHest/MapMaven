using MapMaven.Core.Services.Interfaces;
using System.Reactive.Linq;

namespace MapMaven.Core.Services
{
    public class BeatSaberFileService
    {
        private readonly IApplicationSettingService _applicationSettingService;

        private const string BeatSaberInstallLocationKey = "BeatSaberInstallLocation";

        public string? BeatSaberInstallLocation { get; private set; }
        public IObservable<string?> BeatSaberInstallLocationObservable { get; private set; }

        public virtual string MapsLocation => $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";
        public virtual string PlaylistsLocation => $"{BeatSaberInstallLocation}/Playlists";
        public virtual string UserDataLocation => GetUserDataLocation(BeatSaberInstallLocation);

        public static string AppDataLocation => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MapMaven");
        public static string AppDataCacheLocation => Path.Join(AppDataLocation, "cache");
        public virtual IObservable<string> MapsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Beat Saber_Data/CustomLevels");
        public virtual IObservable<string> PlaylistsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Playlists");
        public virtual IObservable<string> UserDataLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/UserData");

        public static BeatSaberFileService Instance { get; private set; }

        public BeatSaberFileService(IApplicationSettingService applicationSettingService)
        {
            Instance = this;

            _applicationSettingService = applicationSettingService;

            BeatSaberInstallLocationObservable = _applicationSettingService.ApplicationSettings.Select(applicationSettings =>
            {
                BeatSaberInstallLocation = applicationSettings.TryGetValue(BeatSaberInstallLocationKey, out var beatSaberInstallLocation) ? beatSaberInstallLocation.StringValue : null;

                return BeatSaberInstallLocation;
            }).Where(installLocation => !string.IsNullOrEmpty(installLocation));
        }

        public static string GetUserDataLocation(string beatSaberInstallLocation)
        {
            return $"{beatSaberInstallLocation}/UserData";
        }

        public async Task SetBeatSaberInstallLocation(string path)
        {
            path = path.Replace('\\', '/');

            await _applicationSettingService.AddOrUpdateAsync(BeatSaberInstallLocationKey, path);
        }

        public string GetRelativePlaylistPath(string playlistPath)
        {
            var path = Path.GetRelativePath(PlaylistsLocation, playlistPath);

            if (path == ".")
                path = string.Empty;

            path = path.Replace('\\', '/');

            return $"/{path}";
        }
    }
}
