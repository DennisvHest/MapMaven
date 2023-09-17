using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
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

        Task<IEnumerable<MapInfo>> GetAllMapInfo();
        Task<IEnumerable<IPlaylist>> GetAllPlaylists();
        Image GetMapCoverImage(string mapId);
        string GetMapSongPath(string mapId);
        string GetRelativeMapPath(string path);
        Task LoadAllMapInfo();
        Task LoadAllPlaylists();
        Task LoadMapInfo(string id);
        void SetInitialMapLoad(bool initialMapLoad);
    }
}