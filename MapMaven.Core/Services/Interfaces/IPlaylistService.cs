using BeatSaberPlaylistsLib;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Models.Data.Playlists;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Models;
using System.Reactive.Subjects;

namespace MapMaven.Core.Services.Interfaces
{
    public interface IPlaylistService
    {
        IObservable<bool> CreatingPlaylist { get; }
        IObservable<IEnumerable<Playlist>> Playlists { get; }
        BehaviorSubject<Playlist> SelectedPlaylist { get; }
        IObservable<PlaylistTree<Playlist>> PlaylistTree { get; }

        Task<Playlist> AddDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel);
        Task AddMapsToPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true);
        Task AddMapToPlaylist(Map map, Playlist playlist, bool loadPlaylists = true);
        Task<Playlist> AddPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps = null, bool loadPlaylists = true);
        Task<Playlist?> AddPlaylistAndDownloadMaps(EditPlaylistModel editPlaylistModel, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null, CancellationToken cancellationToken = default);
        Task<Playlist?> AddPlaylistAndDownloadMaps(Playlist playlist, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null, CancellationToken cancellationToken = default);
        Task AddPlaylistFolder(string folderName, PlaylistManager? parentPlaylistManager = null);
        Task DeletePlaylist(Playlist playlist, bool deleteMaps = false);
        Task<IEnumerable<Map>> DownloadPlaylistMapsIfNotExist(IEnumerable<Map> playlistMaps, IProgress<ItemProgress<Map>>? progress = null, bool loadMapInfo = true, CancellationToken cancellationToken = default);
        Task<Playlist> EditDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel);
        Task<Playlist> EditPlaylist(EditPlaylistModel editPlaylistModel);
        IEnumerable<PlaylistManager> GetAllPlaylistManagers();
        PlaylistManager GetRootPlaylistManager();
        Task RemoveMapFromPlaylist(Map map, Playlist playlist);
        Task RemoveMapsFromPlaylist(IEnumerable<Map> maps, Playlist playlist);
        Task ReplaceMapsInPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true);
        void SetSelectedPlaylist(Playlist playlist);
    }
}