using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Utilities;
using MapMaven.Models;
using MapMaven.Services;
using Pather.CSharp;
using System.Reactive.Linq;

namespace MapMaven.Core.Services
{
    public class DynamicPlaylistArrangementService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly MapService _mapService;
        private readonly PlaylistService _playlistService;
        private readonly ScoreSaberService _scoreSaberService;
        private readonly ApplicationSettingService _applicationSettingService;

        private readonly IResolver _resolver;

        public DynamicPlaylistArrangementService(BeatSaberDataService beatSaberDataService, MapService mapService, PlaylistService playlistService, ScoreSaberService scoreSaberService, ApplicationSettingService applicationSettingService)
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
                    FilterOperator.Equals => stringValue == filterOperation.Value,
                    FilterOperator.NotEquals => stringValue != filterOperation.Value,
                    _ => false
                };
            }

            if (value is double doubleValue)
            {
                var compareValue = double.Parse(filterOperation.Value);

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

            if (value is TimeSpan timeSpanValue)
            {
                var compareValue = TimeSpan.Parse(filterOperation.Value);

                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => timeSpanValue == compareValue,
                    FilterOperator.NotEquals => timeSpanValue != compareValue,
                    FilterOperator.GreaterThan => timeSpanValue > compareValue,
                    FilterOperator.LessThan => timeSpanValue < compareValue,
                    FilterOperator.LessThanOrEqual => timeSpanValue <= compareValue,
                    FilterOperator.GreaterThanOrEqual => timeSpanValue >= compareValue,
                    _ => false
                };
            }

            if (value is DateTimeOffset dateTimeValue)
            {
                var compareValue = DateTimeOffset.Parse(filterOperation.Value);

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
