﻿using BeatSaberTools.Core.Models.DynamicPlaylists;
using BeatSaberTools.Models;
using BeatSaberTools.Services;
using Pather.CSharp;
using System.Reactive.Linq;

namespace BeatSaberTools.Core.Services
{
    public class DynamicPlaylistArrangementService
    {
        private readonly BeatSaberDataService _beatSaberDataService;
        private readonly MapService _mapService;
        private readonly PlaylistService _playlistService;
        private readonly ScoreSaberService _scoreSaberService;

        private readonly IResolver _resolver;

        public DynamicPlaylistArrangementService(BeatSaberDataService beatSaberDataService, MapService mapService, PlaylistService playlistService, ScoreSaberService scoreSaberService)
        {
            _beatSaberDataService = beatSaberDataService;
            _mapService = mapService;
            _playlistService = playlistService;
            _scoreSaberService = scoreSaberService;

            _resolver = new Resolver();
        }

        public async Task ArrangeDynamicPlaylists()
        {
            var playlists = await _beatSaberDataService.GetAllPlaylists();

            var dynamicPlaylists = playlists
                .Select(p => new
                {
                    Playlist = new Playlist(p),
                    PlaylistInfo = p
                })
                .Where(x => x.Playlist.IsDynamicPlaylist);

            if (!dynamicPlaylists.Any())
                return;

            await Task.WhenAll(new[] {
                _beatSaberDataService.LoadAllMapInfo(),
                _scoreSaberService.LoadPlayerData(),
                _scoreSaberService.LoadRankedMaps(),
                _beatSaberDataService.LoadAllPlaylists()
            });

            var maps = await _mapService.CompleteMapData.FirstAsync();

            foreach (var playlist in dynamicPlaylists)
            {
                var configuration = playlist.Playlist.DynamicPlaylistConfiguration;

                maps = FilterMaps(maps, configuration);
                maps = SortMaps(maps, configuration);

                maps = maps.Take(configuration.MapCount);

                await _playlistService.ReplaceMapsInPlaylist(maps, playlist.Playlist, loadPlaylists: false);
            }
        }

        private IEnumerable<Map> FilterMaps(IEnumerable<Map> maps, DynamicPlaylistConfiguration configuration)
        {
            foreach (var filterOperation in configuration.FilterOperations)
            {
                maps = maps.Where(map => FilterOperationMatches(map, filterOperation));
            }

            return maps;
        }

        private bool FilterOperationMatches(Map map, FilterOperation filterOperation)
        {
            var value = _resolver.ResolveSafe(map, filterOperation.Field);

            if (value is string stringValue)
            {
                return filterOperation.Operator switch
                {
                    FilterOperator.Equals => stringValue == filterOperation.Value,
                    FilterOperator.NotEquals => stringValue != filterOperation.Value,
                    _ => true
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
                    _ => true
                };
            }

            return true;
        }

        private IEnumerable<Map> SortMaps(IEnumerable<Map> maps, DynamicPlaylistConfiguration configuration)
        {
            var firstSortOperation = configuration.SortOperations.FirstOrDefault();

            if (firstSortOperation != null)
            {
                IOrderedEnumerable<Map> orderedMaps;

                if (firstSortOperation.Direction == SortDirection.Ascending)
                {
                    orderedMaps = maps.OrderBy(m => _resolver.ResolveSafe(m, firstSortOperation.Field));
                }
                else
                {
                    orderedMaps = maps.OrderByDescending(m => _resolver.ResolveSafe(m, firstSortOperation.Field));
                }

                foreach (var otherSortOperation in configuration.SortOperations.Skip(1))
                {
                    if (otherSortOperation.Direction == SortDirection.Ascending)
                    {
                        orderedMaps = orderedMaps.ThenBy(m => _resolver.ResolveSafe(m, otherSortOperation.Field));
                    }
                    else
                    {
                        orderedMaps = orderedMaps.ThenByDescending(m => _resolver.ResolveSafe(m, otherSortOperation.Field));
                    }
                }

                maps = orderedMaps;
            }

            return maps;
        }
    }
}