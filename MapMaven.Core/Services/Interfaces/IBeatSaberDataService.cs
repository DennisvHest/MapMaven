using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.Data.Playlists;
using MapMaven.Models.Data;
using System.Drawing;

namespace MapMaven.Core.Services.Interfaces
{
    public interface IBeatSaberDataService
    {
        IObservable<bool> InitialMapLoad { get; }
        IObservable<bool> LoadingMapInfo { get; }
        IObservable<bool> LoadingPlaylistInfo { get; }
        IObservable<IEnumerable<MapInfo>> MapInfo { get; }
        IObservable<Dictionary<string, MapInfo>> MapInfoByHash { get; }
        IObservable<IEnumerable<IPlaylist>> PlaylistInfo { get; }
        PlaylistManager PlaylistManager { get; }
        IObservable<PlaylistTree<IPlaylist>> PlaylistTree { get; }

        Task ClearMapCache();
        Task DeleteMap(string mapHash);
        Task DeleteMaps(IEnumerable<string> mapHashes);
        Task<IEnumerable<MapInfo>> GetAllMapInfo();
        Task<IEnumerable<IPlaylist>> GetAllPlaylists();
        Image GetMapCoverImage(string mapId);
        string GetMapCoverImageFilePath(string mapId);
        Stream GetMapCoverImageStream(string mapId);
        string GetMapSongPath(string mapId);
        string GetRelativeMapPath(string path);
        Task LoadAllMapInfo();
        Task LoadAllPlaylists();
        Task LoadMapInfo(string id);
        bool MapIsLoaded(string mapHash);
        void SetInitialMapLoad(bool initialMapLoad);
    }
}