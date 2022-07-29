using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib;
using BeatSaberTools.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = BeatSaberTools.Models.Playlist;

namespace BeatSaberTools.Services
{
    public class PlaylistService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly PlaylistManager _playlistManager;

        private readonly BehaviorSubject<Playlist> _selectedPlaylist = new(null);

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public IObservable<Playlist> SelectedPlaylist => _selectedPlaylist;

        public PlaylistService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;
            _playlistManager = new PlaylistManager(BeatSaberDataService.PlaylistsLocation, new LegacyPlaylistHandler());

            Playlists = _beatSaberDataService.PlaylistInfo.Select(x => x.Select(i => new Playlist(i)));
        }

        public void SetSelectedPlaylist(Playlist playlist)
        {
            _selectedPlaylist.OnNext(playlist);
        }

        public async Task<Playlist> AddPlaylist(AddPlaylistModel addPlaylistModel)
        {
            var addedPlaylist = _playlistManager.CreatePlaylist(addPlaylistModel.Name, addPlaylistModel.Name, "DennisvHest", coverImage: addPlaylistModel.CoverImage);
            _playlistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist);

            await _beatSaberDataService.LoadAllPlaylists();

            return playlist;
        }

        public async Task DeletePlaylist(Playlist playlist)
        {
            var playlistToDelete = _playlistManager.GetPlaylist(playlist.FileName);
            _playlistManager.DeletePlaylist(playlistToDelete);

            if (playlist.FileName == _selectedPlaylist.Value?.FileName)
                _selectedPlaylist.OnNext(null); // Playlist should not be selected if deleted.

            await _beatSaberDataService.LoadAllPlaylists();
        }
    }
}
