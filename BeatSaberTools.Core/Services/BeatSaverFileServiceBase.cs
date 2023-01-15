using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class BeatSaverFileService
    {
        public string? BeatSaberInstallLocation => _beatSaberInstallLocation.Value;

        private BehaviorSubject<string?> _beatSaberInstallLocation = new(null);
        public IObservable<string?> BeatSaberInstallLocationObservable => _beatSaberInstallLocation;

        public virtual string MapsLocation => $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";
        public virtual string PlaylistsLocation => $"{BeatSaberInstallLocation}/Playlists";
        public virtual string UserDataLocation => $"{BeatSaberInstallLocation}/UserData";
        public static string AppDataLocation => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BSTools");
        public virtual IObservable<string> MapsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Beat Saber_Data/CustomLevels");
        public virtual IObservable<string> PlaylistsLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/Playlists");
        public virtual IObservable<string> UserDataLocationObservable => BeatSaberInstallLocationObservable.Select(location => $"{location}/UserData");


        public void SetBeatSaberInstallLocation(string path)
        {
            path = path.Replace('\\', '/');

            _beatSaberInstallLocation.OnNext(path);
        }
    }
}
