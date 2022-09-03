using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib;
using BeatSaberTools.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = BeatSaberTools.Models.Playlist;
using Map = BeatSaberTools.Models.Map;
using BeatSaberTools.Core.Services;
using BeatSaberPlaylistsLib.Types;

namespace BeatSaberTools.Services
{
    public class PlaylistService
    {
        private readonly IBeatSaverFileService _beatSaverFileService;

        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly PlaylistManager _playlistManager;

        private readonly BehaviorSubject<string> _selectedPlaylistFileName = new(null);

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public BehaviorSubject<Playlist> SelectedPlaylist = new(null);

        public PlaylistService(BeatSaberDataService beatSaberDataService, IBeatSaverFileService beatSaverFileService)
        {
            _beatSaverFileService = beatSaverFileService;
            _beatSaberDataService = beatSaberDataService;
            _playlistManager = new PlaylistManager(_beatSaverFileService.PlaylistsLocation, new LegacyPlaylistHandler());

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

        public async Task<Playlist> AddDynamicPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps = null, bool loadPlaylists = true)
        {
            var playlist = await CreatePlaylist(editPlaylistModel, playlistMaps, dynamicPlaylist: true);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();

            return playlist;
        }

        public async Task<Playlist> AddPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps = null, bool loadPlaylists = true)
        {
            var playlist = await CreatePlaylist(editPlaylistModel, playlistMaps, dynamicPlaylist: false);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();

            return playlist;
        }

        private async Task<Playlist> CreatePlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps, bool dynamicPlaylist)
        {
            if (playlistMaps == null)
                playlistMaps = Array.Empty<Map>();

            var addedPlaylist = _playlistManager.CreatePlaylist(
                fileName: editPlaylistModel.Name,
                title: editPlaylistModel.Name,
                author: "Beat Saber Tools",
                coverImage: editPlaylistModel.CoverImage,
                description: editPlaylistModel.Description
            );

            if (dynamicPlaylist)
            {
                addedPlaylist.SetCustomData("beatSaberTools", new
                {
                    isDynamicPlaylist = dynamicPlaylist
                });
            }

            foreach (var map in playlistMaps)
            {
                addedPlaylist.Add(
                    songHash: map.Hash,
                    songName: map.Name,
                    mapper: map.MapAuthorName,
                    songKey: null
                );
            }

            _playlistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist);

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

        public async Task EditPlaylist(EditPlaylistModel editPlaylistModel)
        {
            var playlistToModify = _playlistManager.GetPlaylist(editPlaylistModel.FileName);

            playlistToModify.Title = editPlaylistModel.Name;
            playlistToModify.Description = editPlaylistModel.Description;
            playlistToModify.SetCover(editPlaylistModel.CoverImage);

            _playlistManager.StorePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task AddMapToPlaylist(Map map, Playlist playlist, bool loadPlaylists = true)
        {
            await AddMapsToPlaylist(new Map[] { map }, playlist, loadPlaylists);
        }

        public async Task AddMapsToPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true)
        {
            var playlistToModify = _playlistManager.GetPlaylist(playlist.FileName);

            await AddMapsToPlaylist(maps, playlistToModify, loadPlaylists);
        }

        private async Task AddMapsToPlaylist(IEnumerable<Map> maps, IPlaylist? playlistToModify, bool loadPlaylists)
        {
            foreach (var map in maps)
            {
                playlistToModify.Add(
                    songHash: map.Hash,
                    songName: map.Name,
                    mapper: map.MapAuthorName,
                    songKey: null
                );
            }

            _playlistManager.StorePlaylist(playlistToModify);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task ReplaceMapsInPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true)
        {
            var playlistToModify = _playlistManager.GetPlaylist(playlist.FileName);

            playlistToModify.Clear();

            await AddMapsToPlaylist(maps, playlistToModify, loadPlaylists);
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
