using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Models;
using Pather.CSharp;
using System.Reactive.Linq;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Models.AdvancedSearch;

namespace MapMaven.Core.Services
{
    public class DynamicPlaylistArrangementService
    {
        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly IMapService _mapService;
        private readonly IPlaylistService _playlistService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly IApplicationSettingService _applicationSettingService;

        private readonly ILogger<DynamicPlaylistArrangementService> _logger;

        private readonly IResolver _resolver;

        private readonly BehaviorSubject<bool> _loadingMapInfo = new(false);
        public IObservable<bool> ArrangingDynamicPlaylists => _loadingMapInfo;

        public static readonly Dictionary<Type, IEnumerable<FilterOperator>> FilterOperatorsForType = new()
        {
            { typeof(string), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.Contains, FilterOperator.NotContains } },
            { typeof(bool), new[] { FilterOperator.Equals, FilterOperator.NotEquals } },
            { typeof(double), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.GreaterThanOrEqual, FilterOperator.LessThanOrEqual } },
            { typeof(DateTime), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.GreaterThanOrEqual, FilterOperator.LessThanOrEqual } },
            { typeof(IEnumerable<string>), new[] { FilterOperator.Contains, FilterOperator.NotContains } },
        };

        public DynamicPlaylistArrangementService(
            IBeatSaberDataService beatSaberDataService,
            IMapService mapService,
            IPlaylistService playlistService,
            ILeaderboardService scoreSaberService,
            IApplicationSettingService applicationSettingService,
            ILogger<DynamicPlaylistArrangementService> logger)
        {
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;
            _playlistService = playlistService;
            _leaderboardService = scoreSaberService;

            _logger = logger;

            _resolver = new Resolver();
            _applicationSettingService = applicationSettingService;
        }

        public async Task ArrangeDynamicPlaylists()
        {
            try
            {
                _loadingMapInfo.OnNext(true);

                await _applicationSettingService.LoadAsync();

                var playlists = await _beatSaberDataService.GetAllPlaylists();

                var dynamicPlaylists = playlists
                    .Select(p => new
                    {
                        Playlist = new Playlist(p),
                        PlaylistInfo = p
                    })
                    .Where(x => x.Playlist.IsDynamicPlaylist);

                await _mapService.RefreshDataAsync(reloadMapAndLeaderboardInfo: true);

                if (!dynamicPlaylists.Any())
                    return;

                var mapData = await _mapService.CompleteMapData.FirstAsync();

                var rankedMapsTasks = _leaderboardService.LeaderboardProviders.Select(async leaderboard =>
                {
                    IEnumerable<DynamicPlaylistMapPair>? rankedMaps = null;

                    if (leaderboard.Key is not null)
                    {
                        try
                        {
                            var rankedMapsData = await _mapService.GetCompleteRankedMapDataForLeaderboardProvider(leaderboard.Key.Value);
                            rankedMaps = rankedMapsData.Select(m => new DynamicPlaylistMapPair
                            {
                                DynamicPlaylistMap = new AdvancedSearchMap(m),
                                Map = m
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to get ranked map data in dynamic playlist arrangement.");
                        }
                    }

                    if (rankedMaps is null)
                        rankedMaps = Enumerable.Empty<DynamicPlaylistMapPair>();

                    return (leaderboardProvider: leaderboard.Key, rankedMaps);
                });

                var rankedMaps = await Task.WhenAll(rankedMapsTasks);
                var rankedMapsPerLeaderboardProvider = rankedMaps.ToDictionary(x => x.leaderboardProvider, x => x.rankedMaps);

                var maps = mapData.Select(m => new DynamicPlaylistMapPair
                {
                    DynamicPlaylistMap = new AdvancedSearchMap(m),
                    Map = m
                });

                foreach (var playlist in dynamicPlaylists)
                {
                    try
                    {
                        var configuration = playlist.Playlist.DynamicPlaylistConfiguration;

                        var rankedMapsForLeaderboard = Enumerable.Empty<DynamicPlaylistMapPair>();

                        if (configuration.LeaderboardProvider is not null && rankedMapsPerLeaderboardProvider.ContainsKey(configuration.LeaderboardProvider))
                            rankedMapsForLeaderboard = rankedMapsPerLeaderboardProvider[playlist.Playlist.DynamicPlaylistConfiguration.LeaderboardProvider];

                        var playlistMaps = configuration.MapPool switch
                        {
                            MapPool.Standard => maps,
                            MapPool.Improvement => rankedMapsForLeaderboard,
                            _ => maps
                        };

                        playlistMaps = FilterMaps(playlistMaps, configuration);
                        playlistMaps = MapSearchService.SortMaps(playlistMaps, configuration.SortOperations, x => x.DynamicPlaylistMap);

                        var resultPlaylistMaps = playlistMaps
                            .Select(m => m.Map)
                            .DistinctBy(m => m.Hash)
                            .Take(configuration.MapCount)
                            .ToList();

                        await _playlistService.DownloadPlaylistMapsIfNotExist(resultPlaylistMaps, loadMapInfo: false);

                        await _playlistService.ReplaceMapsInPlaylist(resultPlaylistMaps, playlist.Playlist, loadPlaylists: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error arranging dynamic playlist: {playlist.PlaylistInfo?.Title}");
                    }
                }

                await Task.WhenAll(new[] {
                    _beatSaberDataService.LoadAllMapInfo(),
                    _beatSaberDataService.LoadAllPlaylists()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error arranging dynamic playlist.");
                throw;
            }
            finally
            {
                _loadingMapInfo.OnNext(false);
            }
        }

        private IEnumerable<DynamicPlaylistMapPair> FilterMaps(IEnumerable<DynamicPlaylistMapPair> maps, DynamicPlaylistConfiguration configuration)
        {
            foreach (var filterOperation in configuration.FilterOperations)
            {
                maps = maps.Where(map => MapSearchService.FilterOperationMatches(map.DynamicPlaylistMap, filterOperation));
            }

            return maps;
        }

        private class DynamicPlaylistMapPair
        {
            public Map Map { get; set; }
            public AdvancedSearchMap DynamicPlaylistMap { get; set; }
        }
    }
}
