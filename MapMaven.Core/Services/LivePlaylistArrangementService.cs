using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Models.LivePlaylists.MapInfo;
using MapMaven.Models;
using Pather.CSharp;
using System.Reactive.Linq;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Models.AdvancedSearch;
using System.Linq;

namespace MapMaven.Core.Services
{
    public class LivePlaylistArrangementService
    {
        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly IMapService _mapService;
        private readonly IPlaylistService _playlistService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly IApplicationSettingService _applicationSettingService;

        private readonly ILogger<LivePlaylistArrangementService> _logger;

        private readonly IResolver _resolver;

        private readonly BehaviorSubject<bool> _loadingMapInfo = new(false);
        public IObservable<bool> ArrangingLivePlaylists => _loadingMapInfo;

        public static readonly Dictionary<Type, IEnumerable<FilterOperator>> FilterOperatorsForType = new()
        {
            { typeof(string), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.Contains, FilterOperator.NotContains } },
            { typeof(bool), new[] { FilterOperator.Equals, FilterOperator.NotEquals } },
            { typeof(double), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.GreaterThanOrEqual, FilterOperator.LessThanOrEqual } },
            { typeof(DateTime), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.GreaterThanOrEqual, FilterOperator.LessThanOrEqual } },
            { typeof(IEnumerable<string>), new[] { FilterOperator.Contains, FilterOperator.NotContains } },
        };

        public LivePlaylistArrangementService(
            IBeatSaberDataService beatSaberDataService,
            IMapService mapService,
            IPlaylistService playlistService,
            ILeaderboardService scoreSaberService,
            IApplicationSettingService applicationSettingService,
            ILogger<LivePlaylistArrangementService> logger)
        {
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;
            _playlistService = playlistService;
            _leaderboardService = scoreSaberService;

            _logger = logger;

            _resolver = new Resolver();
            _applicationSettingService = applicationSettingService;
        }

        public async Task ArrangeLivePlaylists()
        {
            try
            {
                _loadingMapInfo.OnNext(true);

                await _applicationSettingService.LoadAsync();

                await _mapService.RefreshDataAsync(reloadMapAndLeaderboardInfo: true);

                var playlists = await _playlistService.Playlists.FirstAsync();

                var LivePlaylists = playlists.Where(p => p.IsLivePlaylist);

                if (!LivePlaylists.Any())
                    return;

                var mapData = await _mapService.CompleteMapData.FirstAsync();

                var rankedMapsTasks = _leaderboardService.LeaderboardProviders.Select(async leaderboard =>
                {
                    IEnumerable<LivePlaylistMapPair>? rankedMaps = null;

                    if (leaderboard.Key is not null)
                    {
                        try
                        {
                            var rankedMapsData = await _mapService.GetCompleteRankedMapDataForLeaderboardProvider(leaderboard.Key.Value);
                            rankedMaps = rankedMapsData.Select(m => new LivePlaylistMapPair
                            {
                                LivePlaylistMap = new AdvancedSearchMap(m),
                                Map = m
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to get ranked map data in dynamic playlist arrangement.");
                        }
                    }

                    if (rankedMaps is null)
                        rankedMaps = Enumerable.Empty<LivePlaylistMapPair>();

                    return (leaderboardProvider: leaderboard.Key, rankedMaps);
                });

                var rankedMaps = await Task.WhenAll(rankedMapsTasks);
                var rankedMapsPerLeaderboardProvider = rankedMaps.ToDictionary(x => x.leaderboardProvider, x => x.rankedMaps);

                var maps = mapData.Select(m => new LivePlaylistMapPair
                {
                    LivePlaylistMap = new AdvancedSearchMap(m),
                    Map = m
                });

                foreach (var playlist in LivePlaylists)
                {
                    try
                    {
                        var configuration = playlist.LivePlaylistConfiguration;

                        var rankedMapsForLeaderboard = Enumerable.Empty<LivePlaylistMapPair>();

                        if (configuration.LeaderboardProvider is not null && rankedMapsPerLeaderboardProvider.ContainsKey(configuration.LeaderboardProvider))
                            rankedMapsForLeaderboard = rankedMapsPerLeaderboardProvider[playlist.LivePlaylistConfiguration.LeaderboardProvider];

                        var playlistMaps = configuration.MapPool switch
                        {
                            MapPool.Standard => maps,
                            MapPool.Improvement => rankedMapsForLeaderboard,
                            _ => maps
                        };

                        playlistMaps = FilterMaps(playlistMaps, configuration);
                        playlistMaps = MapSearchService.SortMaps(playlistMaps, configuration.SortOperations, x => x.LivePlaylistMap);

                        var resultPlaylistMaps = playlistMaps
                            .Select(m => m.Map)
                            .DistinctBy(m => m.Hash)
                            .Take(configuration.MapCount)
                            .ToList();

                        await _playlistService.DownloadPlaylistMapsIfNotExist(resultPlaylistMaps, loadMapInfo: false);

                        await _playlistService.ReplaceMapsInPlaylist(resultPlaylistMaps, playlist, loadPlaylists: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error arranging dynamic playlist: {playlist?.Title}");
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

        private IEnumerable<LivePlaylistMapPair> FilterMaps(IEnumerable<LivePlaylistMapPair> maps, LivePlaylistConfiguration configuration)
        {
            foreach (var filterOperation in configuration.FilterOperations)
            {
                maps = maps.Where(map => MapSearchService.FilterOperationMatches(map.LivePlaylistMap, filterOperation));
            }

            return maps;
        }

        private class LivePlaylistMapPair
        {
            public Map Map { get; set; }
            public AdvancedSearchMap LivePlaylistMap { get; set; }
        }
    }
}
