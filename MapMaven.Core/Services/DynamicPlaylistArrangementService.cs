using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Models;
using Pather.CSharp;
using System.Reactive.Linq;
using System.Globalization;

namespace MapMaven.Core.Services
{
    public class DynamicPlaylistArrangementService
    {
        private readonly IBeatSaberDataService _beatSaberDataService;
        private readonly IMapService _mapService;
        private readonly IPlaylistService _playlistService;
        private readonly IScoreSaberService _scoreSaberService;
        private readonly IApplicationSettingService _applicationSettingService;

        private readonly IResolver _resolver;

        public static readonly Dictionary<Type, IEnumerable<FilterOperator>> FilterOperatorsForType = new()
        {
            { typeof(string), new[] { FilterOperator.Equals, FilterOperator.NotEquals } },
            { typeof(bool), new[] { FilterOperator.Equals, FilterOperator.NotEquals } },
            { typeof(double), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.GreaterThanOrEqual, FilterOperator.LessThanOrEqual } },
            { typeof(DateTime), new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.GreaterThanOrEqual, FilterOperator.LessThanOrEqual } },
        };

        public DynamicPlaylistArrangementService(
            IBeatSaberDataService beatSaberDataService,
            IMapService mapService,
            IPlaylistService playlistService,
            IScoreSaberService scoreSaberService,
            IApplicationSettingService applicationSettingService)
        {
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;
            _playlistService = playlistService;
            _scoreSaberService = scoreSaberService;

            _resolver = new Resolver();
            _applicationSettingService = applicationSettingService;
        }

        public async Task ArrangeDynamicPlaylists()
        {
            await _applicationSettingService.LoadAsync();

            _playlistService.ResetPlaylistManager();

            var playlists = await _beatSaberDataService.GetAllPlaylists();

            var dynamicPlaylists = playlists
                .Select(p => new
                {
                    Playlist = new Playlist(p),
                    PlaylistInfo = p
                })
                .Where(x => x.Playlist.IsDynamicPlaylist);

            await Task.WhenAll(new[] {
                _beatSaberDataService.LoadAllMapInfo(),
                _scoreSaberService.LoadRankedMaps(),
                _beatSaberDataService.LoadAllPlaylists(),
                _mapService.LoadHiddenMaps()
            });

            if (!dynamicPlaylists.Any())
                return;

            var mapData = await _mapService.CompleteMapData.FirstAsync();
            var rankedMapData = await _mapService.CompleteRankedMapData.FirstAsync();

            var maps = mapData.Select(m => new DynamicPlaylistMapPair
            {
                DynamicPlaylistMap = new DynamicPlaylistMap(m),
                Map = m
            });

            var rankedMaps = rankedMapData.Select(m => new DynamicPlaylistMapPair
            {
                DynamicPlaylistMap = new DynamicPlaylistMap(m),
                Map = m
            });

            foreach (var playlist in dynamicPlaylists)
            {
                var configuration = playlist.Playlist.DynamicPlaylistConfiguration;

                var playlistMaps = configuration.MapPool switch
                {
                    MapPool.Standard => maps,
                    MapPool.Improvement => rankedMaps,
                    _ => maps
                };

                playlistMaps = FilterMaps(playlistMaps, configuration);
                playlistMaps = SortMaps(playlistMaps, configuration);

                playlistMaps = playlistMaps.Take(configuration.MapCount);

                var resultPlaylistMaps = playlistMaps.Select(m => m.Map);

                await _playlistService.DownloadPlaylistMapsIfNotExist(resultPlaylistMaps, loadMapInfo: false);

                await _playlistService.ReplaceMapsInPlaylist(resultPlaylistMaps, playlist.Playlist, loadPlaylists: false);
            }

            await Task.WhenAll(new[] {
                _beatSaberDataService.LoadAllMapInfo(),
                _beatSaberDataService.LoadAllPlaylists()
            });
        }

        private IEnumerable<DynamicPlaylistMapPair> FilterMaps(IEnumerable<DynamicPlaylistMapPair> maps, DynamicPlaylistConfiguration configuration)
        {
            foreach (var filterOperation in configuration.FilterOperations)
            {
                maps = maps.Where(map => FilterOperationMatches(map, filterOperation));
            }

            return maps;
        }

        private bool FilterOperationMatches(DynamicPlaylistMapPair mapPair, FilterOperation filterOperation)
        {
            var map = mapPair.DynamicPlaylistMap;

            var value = _resolver.ResolveSafe(map, filterOperation.Field);

            if (value is string stringValue)
            {
                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => stringValue.Equals(filterOperation.Value, StringComparison.OrdinalIgnoreCase),
                    FilterOperator.NotEquals => !stringValue.Equals(filterOperation.Value, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }

            if (value is double doubleValue)
            {
                var compareValue = double.Parse(filterOperation.Value, CultureInfo.InvariantCulture);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => doubleValue == compareValue,
                    FilterOperator.NotEquals => doubleValue != compareValue,
                    FilterOperator.GreaterThan => doubleValue > compareValue,
                    FilterOperator.LessThan => doubleValue < compareValue,
                    FilterOperator.LessThanOrEqual => doubleValue <= compareValue,
                    FilterOperator.GreaterThanOrEqual => doubleValue >= compareValue,
                    _ => false
                };
            }

            if (value is DateTime dateTimeValue)
            {
                var compareValue = DateTime.Parse(filterOperation.Value);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => dateTimeValue == compareValue,
                    FilterOperator.NotEquals => dateTimeValue != compareValue,
                    FilterOperator.GreaterThan => dateTimeValue > compareValue,
                    FilterOperator.LessThan => dateTimeValue < compareValue,
                    FilterOperator.LessThanOrEqual => dateTimeValue <= compareValue,
                    FilterOperator.GreaterThanOrEqual => dateTimeValue >= compareValue,
                    _ => false
                };
            }

            if (value is bool boolValue)
            {
                var compareValue = bool.Parse(filterOperation.Value);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => boolValue == compareValue,
                    FilterOperator.NotEquals => boolValue != compareValue,
                    _ => false
                };
            }

            return false;
        }

        private IEnumerable<DynamicPlaylistMapPair> SortMaps(IEnumerable<DynamicPlaylistMapPair> maps, DynamicPlaylistConfiguration configuration)
        {
            var firstSortOperation = configuration.SortOperations.FirstOrDefault();

            if (firstSortOperation != null)
            {
                IOrderedEnumerable<DynamicPlaylistMapPair> orderedMaps;

                if (firstSortOperation.Direction == SortDirection.Ascending)
                {
                    orderedMaps = maps.OrderBy(m => _resolver.ResolveSafe(m.DynamicPlaylistMap, firstSortOperation.Field));
                }
                else
                {
                    orderedMaps = maps.OrderByDescending(m => _resolver.ResolveSafe(m.DynamicPlaylistMap, firstSortOperation.Field));
                }

                foreach (var otherSortOperation in configuration.SortOperations.Skip(1))
                {
                    if (otherSortOperation.Direction == SortDirection.Ascending)
                    {
                        orderedMaps = orderedMaps.ThenBy(m => _resolver.ResolveSafe(m.DynamicPlaylistMap, otherSortOperation.Field));
                    }
                    else
                    {
                        orderedMaps = orderedMaps.ThenByDescending(m => _resolver.ResolveSafe(m.DynamicPlaylistMap, otherSortOperation.Field));
                    }
                }

                maps = orderedMaps;
            }

            return maps;
        }

        private class DynamicPlaylistMapPair
        {
            public Map Map { get; set; }
            public DynamicPlaylistMap DynamicPlaylistMap { get; set; }
        }
    }
}
