using System.Reactive.Linq;

namespace BeatSaberTools.Core.Services
{
    public interface IBeatSaverFileService
    {
        public string BeatSaberInstallLocation { get; }
        public string MapsLocation => $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";
        public string PlaylistsLocation => $"{BeatSaberInstallLocation}/Playlists";
        public string UserDataLocation => $"{BeatSaberInstallLocation}/UserData";
        public string MapInfoCachePath { get; }
        IObservable<string> BeatSaberInstallLocationObservable { get; }
        IObservable<string> MapsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Beat Saber_Data/CustomLevels");
        IObservable<string> PlaylistsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Playlists");
        IObservable<string> UserDataLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/UserData");

        public void SetBeatSaberInstallLocation(string path);
    }
}
