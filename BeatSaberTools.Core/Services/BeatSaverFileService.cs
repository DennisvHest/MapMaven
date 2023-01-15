using BeatSaberTools.Core.Extensions;
using BeatSaberTools.Core.Models.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BeatSaberTools.Core.Services
{
    public class BeatSaverFileService
    {
        private IServiceProvider _serviceProvider;

        private const string BeatSaberInstallLocationKey = "BeatSaberInstallLocation";

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

        public BeatSaverFileService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public async Task SetBeatSaberInstallLocation(string path)
        {
            path = path.Replace('\\', '/');

            using var scope = _serviceProvider.CreateScope();

            var dataStore = scope.ServiceProvider.GetRequiredService<IDataStore>();

            await dataStore
                .Set<ApplicationSetting>()
                .AddOrUpdateAsync(BeatSaberInstallLocationKey, path);

            await dataStore.SaveChangesAsync();

            _beatSaberInstallLocation.OnNext(path);
        }
    }
}
