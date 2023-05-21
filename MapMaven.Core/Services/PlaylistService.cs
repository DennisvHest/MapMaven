using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib;
using MapMaven.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = MapMaven.Models.Playlist;
using Map = MapMaven.Models.Map;
using MapMaven.Core.Services;
using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.Data;

namespace MapMaven.Services
{
    public class PlaylistService
    {
        private readonly BeatSaberFileService _beatSaverFileService;

        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly MapService _mapService;
        private PlaylistManager _playlistManager;

        private readonly BehaviorSubject<string> _selectedPlaylistFileName = new(null);

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public BehaviorSubject<Playlist> SelectedPlaylist = new(null);

        private readonly BehaviorSubject<bool> _creatingPlaylist = new(false);
        public IObservable<bool> CreatingPlaylist => _creatingPlaylist;

        public PlaylistService(BeatSaberDataService beatSaberDataService, BeatSaberFileService beatSaverFileService, MapService mapService)
        {
            _beatSaverFileService = beatSaverFileService;
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;
            ResetPlaylistManager();

            var playlists = _beatSaberDataService.PlaylistInfo.Select(x =>
                x.Select(i => new Playlist(i))
                .OrderBy(p => p.Title)
                .ToList()
            ).Replay(1);

            playlists.Connect();

            Playlists = playlists;

            Observable.CombineLatest(Playlists, _selectedPlaylistFileName, (playlists, selectedPlaylistFileName) => (playlists, selectedPlaylistFileName))
                .Select(x =>
                {
                    if (string.IsNullOrEmpty(x.selectedPlaylistFileName))
                        return null;

                    return x.playlists.FirstOrDefault(p => p.FileName == x.selectedPlaylistFileName);
                })
                .Subscribe(SelectedPlaylist.OnNext); // Subscribing here because of weird behavior with multiple subscriptions triggering multiple reruns of this observable.

            _beatSaverFileService.BeatSaberInstallLocationObservable
                .DistinctUntilChanged()
                .Subscribe(_ => ResetPlaylistManager());
        }

        public void ResetPlaylistManager()
        {
            _playlistManager = new PlaylistManager(_beatSaverFileService.PlaylistsLocation, new LegacyPlaylistHandler());
        }

        public void SetSelectedPlaylist(Playlist playlist)
        {
            _selectedPlaylistFileName.OnNext(playlist?.FileName);
        }

        public async Task<Playlist> AddPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps = null, bool loadPlaylists = true)
        {
            var playlist = await CreatePlaylist(editPlaylistModel, playlistMaps);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();

            return playlist;
        }

        private async Task<Playlist> CreatePlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps)
        {
            IPlaylist addedPlaylist = CreateIPlaylist(editPlaylistModel, playlistMaps);

            _playlistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist);

            return playlist;
        }

        public async Task<Playlist> AddDynamicPlaylist(EditDynamicPlaylistModel editDynamicPlaylistModel)
        {
            IPlaylist addedPlaylist = CreateIPlaylist(editDynamicPlaylistModel, null);

            addedPlaylist.SetCustomData("mapMaven", new
            {
                isDynamicPlaylist = true,
                dynamicPlaylistConfiguration = editDynamicPlaylistModel.DynamicPlaylistConfiguration
            });

            _playlistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist);

            return playlist;
        }

        public async Task EditDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel)
        {
            var playlistToModify = _playlistManager.GetPlaylist(editPlaylistModel.FileName);
            
            UpdatePlaylist(editPlaylistModel, playlistToModify);

            playlistToModify.SetCustomData("mapMaven", new
            {
                isDynamicPlaylist = true,
                dynamicPlaylistConfiguration = editPlaylistModel.DynamicPlaylistConfiguration
            });

            _playlistManager.StorePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        private static void UpdatePlaylist(EditPlaylistModel editPlaylistModel, IPlaylist? playlistToModify)
        {
            playlistToModify.Title = editPlaylistModel.Name;
            playlistToModify.Description = editPlaylistModel.Description;
            playlistToModify.SetCover(editPlaylistModel.CoverImage);
        }

        private IPlaylist CreateIPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps)
        {
            if (playlistMaps == null)
                playlistMaps = Array.Empty<Map>();

            var addedPlaylist = _playlistManager.CreatePlaylist(
                fileName: editPlaylistModel.FileName ?? editPlaylistModel.Name,
                title: editPlaylistModel.Name,
                author: "Map Maven",
                coverImage: editPlaylistModel.CoverImage,
                description: editPlaylistModel.Description
            );

            foreach (var map in playlistMaps)
            {
                addedPlaylist.Add(
                    songHash: map.Hash,
                    songName: map.Name,
                    mapper: map.MapAuthorName,
                    songKey: null
                );
            }

            return addedPlaylist;
        }

        public async Task AddPlaylistAndDownloadMaps(EditPlaylistModel editPlaylistModel, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null, CancellationToken cancellationToken = default)
        {
            _creatingPlaylist.OnNext(true);

            try
            {
                playlistMaps = await DownloadPlaylistMapsIfNotExist(playlistMaps, progress, cancellationToken: cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                    await AddPlaylist(editPlaylistModel, playlistMaps, loadPlaylists);
            }
            finally
            {
                _creatingPlaylist.OnNext(false);
            }
        }

        public async Task<IEnumerable<Map>> DownloadPlaylistMapsIfNotExist(IEnumerable<Map> playlistMaps, IProgress<ItemProgress<Map>>? progress = null, bool loadMapInfo = true, CancellationToken cancellationToken = default)
        {
            playlistMaps = playlistMaps.ToList();

            ItemProgress<Map>? totalProgress = null;

            if (progress != null)
            {
                totalProgress = new ItemProgress<Map>
                {
                    TotalItems = playlistMaps.Count()
                };

                progress.Report(totalProgress);
            }

            foreach (var map in playlistMaps)
            {
                if (cancellationToken.IsCancellationRequested == true)
                    break;

                Progress<double>? mapProgress = null;

                if (totalProgress != null)
                {
                    totalProgress.CurrentItem = map;

                    mapProgress = new Progress<double>(x =>
                    {
                        totalProgress.CurrentMapProgress = x;
                        progress?.Report(totalProgress);
                    });
                }

                await _mapService.DownloadMap(map, progress: mapProgress, loadMapInfo: loadMapInfo, cancellationToken: cancellationToken);

                if (totalProgress != null)
                    totalProgress.CompletedItems++;
            }

            return playlistMaps;
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

            UpdatePlaylist(editPlaylistModel, playlistToModify);

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
