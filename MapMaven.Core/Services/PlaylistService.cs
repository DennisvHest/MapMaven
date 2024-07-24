﻿using MapMaven.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = MapMaven.Models.Playlist;
using Map = MapMaven.Models.Map;
using MapMaven.Core.Services;
using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Extensions;
using MapMaven.Core.Models.Data.Playlists;
using BeatSaberPlaylistsLib;

namespace MapMaven.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly BeatSaberFileService _beatSaverFileService;

        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly IMapService _mapService;

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
            SelectedPlaylist.OnNext(playlist);
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

            editPlaylistModel.PlaylistManager.SavePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist, editPlaylistModel.PlaylistManager);

            return playlist;
        }

        public async Task<Playlist> AddDynamicPlaylist(EditDynamicPlaylistModel editDynamicPlaylistModel)
        {
            IPlaylist addedPlaylist = CreateIPlaylist(editDynamicPlaylistModel, null);

            CreateValidConfiguration(editDynamicPlaylistModel);

            addedPlaylist.SetCustomData("mapMaven", new
            {
                isDynamicPlaylist = true,
                dynamicPlaylistConfiguration = editDynamicPlaylistModel.DynamicPlaylistConfiguration
            });

            editDynamicPlaylistModel.PlaylistManager.SavePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist, editDynamicPlaylistModel.PlaylistManager);

            return playlist;
        }

        public async Task<Playlist> EditDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel)
        {
            var playlistToModify = editPlaylistModel.PlaylistManager.GetPlaylist(editPlaylistModel.FileName);

            CreateValidConfiguration(editPlaylistModel);
            UpdatePlaylist(editPlaylistModel, playlistToModify);

            playlistToModify.SetCustomData("mapMaven", new
            {
                isDynamicPlaylist = true,
                dynamicPlaylistConfiguration = editPlaylistModel.DynamicPlaylistConfiguration
            });

            editPlaylistModel.PlaylistManager.SavePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();

            return new Playlist(playlistToModify, editPlaylistModel.PlaylistManager);
        }

        public async Task AddPlaylistFolder(string folderName, PlaylistManager? parentPlaylistManager = null)
        {
            var playlistManager = parentPlaylistManager ?? _beatSaberDataService.PlaylistManager;

            playlistManager.CreateChildManager(folderName);

            await _beatSaberDataService.LoadAllPlaylists();
        }

        private static void CreateValidConfiguration(EditDynamicPlaylistModel editDynamicPlaylistModel)
        {
            var configuration = editDynamicPlaylistModel.DynamicPlaylistConfiguration;

            if (configuration.MapPool == MapPool.Standard)
                configuration.LeaderboardProvider = null;
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
            var playlistManager = playlist.PlaylistManager.GetManagerForPlaylist(playlistToDelete);

            playlistManager.DeletePlaylist(playlistToDelete);

            if (playlist == SelectedPlaylist.Value)
                SelectedPlaylist.OnNext(null); // Playlist should not be selected if deleted.

            if (deleteMaps)
                await _mapService.DeleteMaps(playlist.Maps.Select(m => m.Hash));

            await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task<Playlist> EditPlaylist(EditPlaylistModel editPlaylistModel)
        {
            var playlistToModify = editPlaylistModel.PlaylistManager.GetPlaylist(editPlaylistModel.FileName);

            UpdatePlaylist(editPlaylistModel, playlistToModify);

            editPlaylistModel.PlaylistManager.SavePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();

            return new Playlist(playlistToModify, editPlaylistModel.PlaylistManager);
        }

        public async Task AddMapToPlaylist(Map map, Playlist playlist, bool loadPlaylists = true)
        {
            await AddMapsToPlaylist([map], playlist, loadPlaylists);
        }

        public async Task AddMapsToPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true)
        {
            var playlistToModify = playlist.PlaylistManager.GetPlaylist(playlist.FileName);

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

            var playlistManager = _beatSaberDataService.PlaylistManager.GetManagerForPlaylist(playlistToModify);

            playlistManager.SavePlaylist(playlistToModify);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();
        }

        public async Task ReplaceMapsInPlaylist(IEnumerable<Map> maps, Playlist playlist, bool loadPlaylists = true)
        {
            var playlistToModify = playlist.PlaylistManager.GetPlaylist(playlist.FileName);

            playlistToModify.Clear();

            await AddMapsToPlaylist(maps, playlistToModify, loadPlaylists);
        }

        public async Task RemoveMapFromPlaylist(Map map, Playlist playlist) => await RemoveMapsFromPlaylist([map], playlist);

        public async Task RemoveMapsFromPlaylist(IEnumerable<Map> maps, Playlist playlist)
        {
            var playlistToModify = playlist.PlaylistManager.GetPlaylist(playlist.FileName);

            var mapHashes = maps.Select(m => m.Hash).ToList();

            playlistToModify.RemoveAll(m => mapHashes.Contains(m.Hash));

            playlist.PlaylistManager.SavePlaylist(playlistToModify);

            await _beatSaberDataService.LoadAllPlaylists();
        }

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

            while (File.Exists(Path.Join(_beatSaverFileService.PlaylistsLocation, $"{uniqueFileName}.bplist")))
            {
                uniqueFileName = $"{fileName}({count})";
                count++;
            }

            return uniqueFileName;
        }
    }
}
