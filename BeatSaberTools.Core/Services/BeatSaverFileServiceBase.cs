using System.Reactive.Linq;

namespace BeatSaberTools.Core.Services
{
    public abstract class BeatSaverFileServiceBase
    {
        public virtual string BeatSaberInstallLocation { get; }
        public virtual string MapsLocation => $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";
        public virtual string PlaylistsLocation => $"{BeatSaberInstallLocation}/Playlists";
        public virtual string UserDataLocation => $"{BeatSaberInstallLocation}/UserData";
        public static string AppDataLocation => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BSTools");
        public virtual string MapInfoCachePath { get; }
        public virtual string HiddenMapConfigPath { get; }
        public virtual IObservable<string> BeatSaberInstallLocationObservable { get; }
        public virtual IObservable<string> MapsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Beat Saber_Data/CustomLevels");
        public virtual IObservable<string> PlaylistsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Playlists");
        public virtual IObservable<string> UserDataLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/UserData");


        public abstract void SetBeatSaberInstallLocation(string path);
    }
}
