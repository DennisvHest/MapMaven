﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace MapMaven.Core.Models.DynamicPlaylists
{
    public class DynamicPlaylistConfiguration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MapPool MapPool { get; set; } = MapPool.Standard;

        [JsonConverter(typeof(StringEnumConverter))]
        public LeaderboardProvider? LeaderboardProvider { get; set; } = Models.LeaderboardProvider.ScoreSaber;

        [ValidateComplexType]
        public List<FilterOperation> FilterOperations { get; set; } = new();

        [ValidateComplexType]
        public List<SortOperation> SortOperations { get; set; } = new();


        public int MapCount { get; set; }
    }
}
