using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib;
using BeatSaberTools.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = BeatSaberTools.Models.Playlist;
using Map = BeatSaberTools.Models.Map;

namespace BeatSaberTools.Services
{
    public class PlaylistService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly PlaylistManager _playlistManager;

        private readonly BehaviorSubject<string> _selectedPlaylistFileName = new(null);

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public BehaviorSubject<Playlist> SelectedPlaylist = new(null);

        public PlaylistService(BeatSaberDataService beatSaberDataService)
        {
            _beatSaberDataService = beatSaberDataService;
            _playlistManager = new PlaylistManager(BeatSaberDataService.PlaylistsLocation, new LegacyPlaylistHandler());

            Playlists = _beatSaberDataService.PlaylistInfo.Select(x => x.Select(i => new Playlist(i)));

            Observable.CombineLatest(Playlists, _selectedPlaylistFileName, (playlists, selectedPlaylistFileName) => (playlists, selectedPlaylistFileName))
                .Select(x =>
                {
                    if (string.IsNullOrEmpty(x.selectedPlaylistFileName))
                        return null;

                    return x.playlists.FirstOrDefault(p => p.FileName == x.selectedPlaylistFileName);
                })
                .Subscribe(SelectedPlaylist.OnNext); // Subscribing here because of weird behavior with multiple subscriptions triggering multiple reruns of this observable.
        }

        public void SetSelectedPlaylist(Playlist playlist)
        {
            _selectedPlaylistFileName.OnNext(playlist?.FileName);
        }

        public async Task<Playlist> AddPlaylist(AddPlaylistModel addPlaylistModel)
        {
            var addedPlaylist = _playlistManager.CreatePlaylist(
                fileName: addPlaylistModel.Name,
                title: addPlaylistModel.Name,
                author: "Beat Saber Tools",
                coverImage: addPlaylistModel.CoverImage,
                description: addPlaylistModel.Description
             );

            _playlistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist);

            await _beatSaberDataService.LoadAllPlaylists();

            return playlist;
        }

        public async Task DeletePlaylist(Playlist playlist)
        {
            var playlistToDelete = _playlistManager.GetPlaylist(playlist.FileName);
            _playlistManager.DeletePlaylist(playlistToDelete);

            if (playlist.FileName == _selectedPlaylistFileName.Value)
                _selectedPlaylistFileName.OnNext(null); // Playlist should not be selected if deleted.

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task AddMapToPlaylist(Map map, Playlist playlist)
        {
            var playlistToModify = _playlistManager.GetPlaylist(playlist.FileName);

            playlistToModify.Add(
                songHash: map.Hash,
                songName: map.Name,
                mapper: map.MapAuthorName,
                songKey: null
            );

            _playlistManager.StorePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task RemoveMapFromPlaylist(Map map, Playlist playlist)
        {
            var playlistToModify = _playlistManager.GetPlaylist(playlist.FileName);

            var mapToRemove = playlistToModify.FirstOrDefault(m => m.Hash == map.Hash);

            if (mapToRemove == null)
                return;

            playlistToModify.Remove(mapToRemove);

            _playlistManager.StorePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();
        }
    }
}
