using MapMaven.Core.Models.Data;
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

        Task<Playlist> AddDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel);
        Task AddMapsToPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true);
        Task AddMapToPlaylist(Map map, Playlist playlist, bool loadPlaylists = true);
        Task<Playlist> AddPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps = null, bool loadPlaylists = true);
        Task AddPlaylistAndDownloadMaps(EditPlaylistModel editPlaylistModel, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null, CancellationToken cancellationToken = default);
        Task DeletePlaylist(Playlist playlist);
        Task<IEnumerable<Map>> DownloadPlaylistMapsIfNotExist(IEnumerable<Map> playlistMaps, IProgress<ItemProgress<Map>>? progress = null, bool loadMapInfo = true, CancellationToken cancellationToken = default);
        Task EditDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel);
        Task EditPlaylist(EditPlaylistModel editPlaylistModel);
        Task RemoveMapFromPlaylist(Map map, Playlist playlist);
        Task ReplaceMapsInPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true);
        void ResetPlaylistManager();
        void SetSelectedPlaylist(Playlist playlist);
    }
}