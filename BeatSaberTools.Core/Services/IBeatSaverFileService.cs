namespace BeatSaberTools.Core.Services
{
    public interface IBeatSaverFileService
    {
        public string BeatSaberInstallLocation { get; }
        public string MapsLocation => $"{BeatSaberInstallLocation}/Beat Saber_Data/CustomLevels";
        public string PlaylistsLocation => $"{BeatSaberInstallLocation}/Playlists";
        public string UserDataLocation => $"{BeatSaberInstallLocation}/UserData";
        public string MapInfoCachePath { get; }

        public void SetBeatSaberInstallLocation(string path);
    }
}
