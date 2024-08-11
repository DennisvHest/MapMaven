using MapMaven.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = MapMaven.Models.Playlist;
using Map = MapMaven.Models.Map;
using MapMaven.Core.Services;
using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Extensions;
using MapMaven.Core.Models.Data.Playlists;
using BeatSaberPlaylistsLib;
using MapMaven.Core.Models;

namespace MapMaven.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly BeatSaberFileService _beatSaverFileService;

        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly IMapService _mapService;

        private readonly BehaviorSubject<string?> _selectedPlaylistFilePath = new(null);

        public IObservable<PlaylistTree<Playlist>> PlaylistTree { get; private set; }
        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public BehaviorSubject<Playlist?> SelectedPlaylist { get; private set; } = new(null);

        private readonly BehaviorSubject<bool> _creatingPlaylist = new(false);
        public IObservable<bool> CreatingPlaylist => _creatingPlaylist;

        public PlaylistService(IBeatSaberDataService beatSaberDataService, BeatSaberFileService beatSaverFileService, IMapService mapService)
        {
            _beatSaverFileService = beatSaverFileService;
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;

            var playlistTree = _beatSaberDataService.PlaylistTree.Select(tree =>
            {
                var playlistTree = new PlaylistTree<Playlist>(tree.RootPlaylistFolder.PlaylistManager);
                playlistTree.RootPlaylistFolder = MapIPlaylistsToPlaylistsInFolder(tree.RootPlaylistFolder);

                return playlistTree;
            }).Replay(1);

            playlistTree.Connect();

            PlaylistTree = playlistTree;

            Playlists = playlistTree.Select(tree => tree.AllPlaylists().ToList());

            Observable.CombineLatest(Playlists, _selectedPlaylistFilePath, (playlists, selectedPlaylistFilePath) => (playlists, selectedPlaylistFilePath))
                .Select(x =>
                {
                    if (string.IsNullOrEmpty(x.selectedPlaylistFilePath))
                        return null;

                    return x.playlists.FirstOrDefault(p => p.PlaylistFilePath == x.selectedPlaylistFilePath);
                })
                .Subscribe(SelectedPlaylist.OnNext); // Subscribing here because of weird behavior with multiple subscriptions triggering multiple reruns of this observable.
        }

        private PlaylistFolder<Playlist> MapIPlaylistsToPlaylistsInFolder(PlaylistFolder<IPlaylist> folder)
        {
            var playlistFolder = new PlaylistFolder<Playlist>(folder.PlaylistManager);

            foreach (var childFolder in folder.ChildItems.OfType<PlaylistFolder<IPlaylist>>())
            {
                playlistFolder.ChildItems.Add(MapIPlaylistsToPlaylistsInFolder(childFolder));
            }

            var playlistNodes = folder.ChildItems.OfType<PlaylistTreeNode<IPlaylist>>();

            foreach (var playlistNode in playlistNodes)
            {
                playlistFolder.ChildItems.Add(new PlaylistTreeNode<Playlist>(new Playlist(playlistNode.Playlist, folder.PlaylistManager), folder.PlaylistManager));
            }

            return playlistFolder;
        }

        public void SetSelectedPlaylist(Playlist playlist)
        {
            _selectedPlaylistFilePath.OnNext(playlist?.PlaylistFilePath);
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

            editPlaylistModel.PlaylistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist, editPlaylistModel.PlaylistManager);

            return playlist;
        }

        public async Task<Playlist> AddLivePlaylist(EditLivePlaylistModel editLivePlaylistModel)
        {
            IPlaylist addedPlaylist = CreateIPlaylist(editLivePlaylistModel, null);

            CreateValidConfiguration(editLivePlaylistModel);

            addedPlaylist.SetCustomData("mapMaven", new
            {
                isLivePlaylist = true,
                dynamicPlaylistConfiguration = editLivePlaylistModel.LivePlaylistConfiguration
            });

            editLivePlaylistModel.PlaylistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist, editLivePlaylistModel.PlaylistManager);

            return playlist;
        }

        public async Task<Playlist> EditLivePlaylist(EditLivePlaylistModel editPlaylistModel)
        {
            var playlistToModify = editPlaylistModel.OriginalPlaylistManager.GetPlaylist(editPlaylistModel.FileName);

            CreateValidConfiguration(editPlaylistModel);

            playlistToModify.SetCustomData("mapMaven", new
            {
                isLivePlaylist = true,
                dynamicPlaylistConfiguration = editPlaylistModel.LivePlaylistConfiguration
            });

            UpdatePlaylist(editPlaylistModel, playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();

            return new Playlist(playlistToModify, editPlaylistModel.PlaylistManager);
        }

        public async Task AddPlaylistFolder(string folderName, PlaylistManager? parentPlaylistManager = null)
        {
            var playlistManager = parentPlaylistManager ?? _beatSaberDataService.PlaylistManager;

            playlistManager.CreateChildManager(folderName);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task RenamePlaylistFolder(PlaylistManager playlistManager, string newFolderName)
        {
            playlistManager.RenameManager(newFolderName);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task DeletePlaylistFolder(PlaylistManager playlistManager)
        {
            playlistManager.Parent.DeleteChildManager(playlistManager);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        private static void CreateValidConfiguration(EditLivePlaylistModel editLivePlaylistModel)
        {
            var configuration = editLivePlaylistModel.LivePlaylistConfiguration;

            if (configuration.MapPool == MapPool.Standard)
                configuration.LeaderboardProvider = null;
        }

        private void UpdatePlaylist(EditPlaylistModel editPlaylistModel, IPlaylist? playlistToModify)
        {
            playlistToModify.Title = editPlaylistModel.Name;
            playlistToModify.Description = editPlaylistModel.Description;
            playlistToModify.SetCover(editPlaylistModel.CoverImage);

            var movedToOtherFolder = editPlaylistModel.PlaylistManager != editPlaylistModel.OriginalPlaylistManager;

            if (movedToOtherFolder)
            {
                var playlistFileName = GetUniqueFileName(editPlaylistModel); // Prevent overwriting existing playlists with same filename in other folder.

                editPlaylistModel.OriginalPlaylistManager.DeletePlaylist(playlistToModify);

                playlistToModify.Filename = playlistFileName;
            }

            editPlaylistModel.PlaylistManager.StorePlaylist(playlistToModify);
        }

        private IPlaylist CreateIPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps)
        {
            if (playlistMaps == null)
                playlistMaps = Array.Empty<Map>();

            var fileName = GetUniqueFileName(editPlaylistModel);

            var addedPlaylist = editPlaylistModel.PlaylistManager.CreatePlaylist(
                fileName: fileName,
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

        public async Task<Playlist?> AddPlaylistAndDownloadMaps(Playlist playlist, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null, CancellationToken cancellationToken = default)
        {
            return await AddPlaylistAndDownloadMaps(new EditPlaylistModel(playlist), playlistMaps, loadPlaylists, progress, cancellationToken);
        }

        public async Task<Playlist?> AddPlaylistAndDownloadMaps(EditPlaylistModel editPlaylistModel, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null, CancellationToken cancellationToken = default)
        {
            _creatingPlaylist.OnNext(true);

            try
            {
                playlistMaps = await DownloadPlaylistMapsIfNotExist(playlistMaps, progress, cancellationToken: cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                    return await AddPlaylist(editPlaylistModel, playlistMaps, loadPlaylists);
            }
            finally
            {
                _creatingPlaylist.OnNext(false);
            }

            return null;
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
                {
                    totalProgress.CurrentMapProgress = 0;
                    totalProgress.CompletedItems++;
                    progress?.Report(totalProgress);
                }
            }

            return playlistMaps;
        }

        public async Task DeletePlaylist(Playlist playlist, bool deleteMaps = false)
        {
            var playlistToDelete = playlist.PlaylistManager.GetPlaylist(playlist.FileName);

            playlist.PlaylistManager.DeletePlaylist(playlistToDelete);

            if (playlist == SelectedPlaylist.Value)
                SelectedPlaylist.OnNext(null); // Playlist should not be selected if deleted.

            if (deleteMaps)
                await _mapService.DeleteMaps(playlist.Maps.Select(m => m.Hash));

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task<Playlist> EditPlaylist(EditPlaylistModel editPlaylistModel)
        {
            var playlistToModify = editPlaylistModel.OriginalPlaylistManager.GetPlaylist(editPlaylistModel.FileName);

            UpdatePlaylist(editPlaylistModel, playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();

            return new Playlist(playlistToModify, editPlaylistModel.PlaylistManager);
        }

        public async Task AddMapToPlaylist(Map map, Playlist playlist, bool loadPlaylists = true)
        {
            await AddMapsToPlaylist([map], playlist, loadPlaylists);
        }

        public async Task AddMapsToPlaylist(IEnumerable<Map> maps, Playlist playlistToModify, bool loadPlaylists = true)
        {
            var iPlaylist = playlistToModify.PlaylistManager.GetPlaylist(playlistToModify.FileName);

            foreach (var map in maps)
            {
                iPlaylist.Add(
                    songHash: map.Hash,
                    songName: map.Name,
                    mapper: map.MapAuthorName,
                    songKey: null
                );
            }

            playlistToModify.PlaylistManager.StorePlaylist(iPlaylist);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task ReplaceMapsInPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true)
        {
            var playlistToModify = playlist.PlaylistManager.GetPlaylist(playlist.FileName);

            playlistToModify.Clear();

            await AddMapsToPlaylist(maps, playlist, loadPlaylists);
        }

        public async Task RemoveMapFromPlaylist(Map map, Playlist playlist) => await RemoveMapsFromPlaylist([map], playlist);

        public async Task RemoveMapsFromPlaylist(IEnumerable<Map> maps, Playlist playlist)
        {
            var playlistToModify = playlist.PlaylistManager.GetPlaylist(playlist.FileName);

            var mapHashes = maps.Select(m => m.Hash).ToList();

            playlistToModify.RemoveAll(m => mapHashes.Contains(m.Hash));

            playlist.PlaylistManager.StorePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public static PlaylistFolder<Playlist> FilterPlaylistFolder(PlaylistFolder<Playlist> playlistFolder, string searchText, PlaylistType? playlistType = null, bool includeEmptyFolders = false)
        {
            if (string.IsNullOrEmpty(searchText) && playlistType is null)
                return playlistFolder;

            playlistFolder.ChildItems = playlistFolder.ChildItems
                .Where(item => PlaylistTreeItemContainsItem(item, searchText, playlistType, includeEmptyFolders))
                .ToList();

            foreach (var item in playlistFolder.ChildItems)
            {
                if (item is PlaylistFolder<Playlist> childFolder)
                {
                    childFolder = FilterPlaylistFolder(childFolder, searchText, playlistType, includeEmptyFolders);
                }
            }

            return playlistFolder;
        }

        public static bool PlaylistTreeItemContainsItem(PlaylistTreeItem<Playlist> item, string searchText, PlaylistType? playlistType, bool includeEmptyFolders = false) => item switch
        {
            PlaylistFolder<Playlist> childFolder =>
                !string.IsNullOrEmpty(searchText) && childFolder.FolderName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || childFolder.ChildItems.Any(item => PlaylistTreeItemContainsItem(item, searchText, playlistType, includeEmptyFolders))
                || includeEmptyFolders && string.IsNullOrEmpty(searchText) && !childFolder.ChildItems.Any(),
            PlaylistTreeNode<Playlist> playlist =>
                (string.IsNullOrEmpty(searchText) || playlist.Playlist.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                && (
                    playlistType is null
                    || playlistType == PlaylistType.Playlist && !playlist.Playlist.IsLivePlaylist
                    || playlistType == PlaylistType.LivePlaylist && playlist.Playlist.IsLivePlaylist
                ),
            _ => false
        };

        public PlaylistManager GetRootPlaylistManager() => _beatSaberDataService.PlaylistManager;

        public IEnumerable<PlaylistManager> GetAllPlaylistManagers()
        {
            return new[] { _beatSaberDataService.PlaylistManager }
                .Union(GetAllPlaylistManagers(_beatSaberDataService.PlaylistManager)
                .OrderBy(m => m.PlaylistPath));
        }

        private IEnumerable<PlaylistManager> GetAllPlaylistManagers(PlaylistManager playlistManager)
        {
            var childManagers = playlistManager.GetChildManagers();

            return childManagers.Union(childManagers.SelectMany(GetAllPlaylistManagers));
        }

        private string GetUniqueFileName(EditPlaylistModel editPlaylistModel)
        {
            var fileName = editPlaylistModel.FileName ?? editPlaylistModel.Name;
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

            string uniqueFileName = fileName;
            int count = 1;

            while (File.Exists(Path.Join(editPlaylistModel.PlaylistManager.PlaylistPath, $"{uniqueFileName}.bplist")))
            {
                uniqueFileName = $"{fileName}({count})";
                count++;
            }

            return uniqueFileName;
        }
    }
}
