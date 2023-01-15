﻿using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib;
using BeatSaberTools.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Playlist = BeatSaberTools.Models.Playlist;
using Map = BeatSaberTools.Models.Map;
using BeatSaberTools.Core.Services;
using BeatSaberPlaylistsLib.Types;
using BeatSaberTools.Core.Models.DynamicPlaylists;
using BeatSaberTools.Core.Models.Data;

namespace BeatSaberTools.Services
{
    public class PlaylistService
    {
        private readonly BeatSaverFileService _beatSaverFileService;

        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly MapService _mapService;
        private readonly PlaylistManager _playlistManager;

        private readonly BehaviorSubject<string> _selectedPlaylistFileName = new(null);

        public IObservable<IEnumerable<Playlist>> Playlists { get; private set; }
        public BehaviorSubject<Playlist> SelectedPlaylist = new(null);

        private readonly BehaviorSubject<bool> _creatingPlaylist = new(false);
        public IObservable<bool> CreatingPlaylist => _creatingPlaylist;

        public PlaylistService(BeatSaberDataService beatSaberDataService, BeatSaverFileService beatSaverFileService, MapService mapService)
        {
            _beatSaverFileService = beatSaverFileService;
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;
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

        public async Task<Playlist> AddDynamicPlaylist(EditDynamicPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps = null, bool loadPlaylists = true)
        {
            var playlist = await CreateDynamicPlaylist(editPlaylistModel, playlistMaps);

            if (loadPlaylists)
                await _beatSaberDataService.LoadAllPlaylists();

            return playlist;
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

        private async Task<Playlist> CreateDynamicPlaylist(EditDynamicPlaylistModel editDynamicPlaylistModel, IEnumerable<Map>? playlistMaps)
        {
            IPlaylist addedPlaylist = CreateIPlaylist(editDynamicPlaylistModel, playlistMaps);

            addedPlaylist.SetCustomData("beatSaberTools", new
            {
                isDynamicPlaylist = true,
                dynamicPlaylistConfiguration = editDynamicPlaylistModel.DynamicPlaylistConfiguration
            });

            _playlistManager.StorePlaylist(addedPlaylist);

            var playlist = new Playlist(addedPlaylist);

            return playlist;
        }

        private IPlaylist CreateIPlaylist(EditPlaylistModel editPlaylistModel, IEnumerable<Map>? playlistMaps)
        {
            if (playlistMaps == null)
                playlistMaps = Array.Empty<Map>();

            var addedPlaylist = _playlistManager.CreatePlaylist(
                fileName: editPlaylistModel.FileName ?? editPlaylistModel.Name,
                title: editPlaylistModel.Name,
                author: "Beat Saber Tools",
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

        public async Task AddPlaylistAndDownloadMaps(EditPlaylistModel editPlaylistModel, IEnumerable<Map> playlistMaps, bool loadPlaylists = true, IProgress<ItemProgress<Map>>? progress = null)
        {
            _creatingPlaylist.OnNext(true);

            playlistMaps = playlistMaps.ToList();

            try
            {
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
                    Progress<double>? mapProgress = null;

                    if (totalProgress != null)
                    {
                        totalProgress.CurrentItem = map;

                        mapProgress = new Progress<double>(x =>
                        {
                            totalProgress.CurrentMapProgress = x;
                            progress.Report(totalProgress);
                        });
                    }

                    await _mapService.DownloadMap(map, progress: mapProgress);

                    if (totalProgress != null)
                        totalProgress.CompletedItems++;
                }

                await AddPlaylist(editPlaylistModel, playlistMaps, loadPlaylists);
            }
            finally
            {
                _creatingPlaylist.OnNext(false);
            }
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
