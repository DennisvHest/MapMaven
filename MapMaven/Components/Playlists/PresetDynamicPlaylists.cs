﻿using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;

namespace MapMaven.Components.Playlists
{
    public static class PresetDynamicPlaylists
    {
        public static List<EditDynamicPlaylistModel> Playlists = new List<EditDynamicPlaylistModel>
        {
            new EditDynamicPlaylistModel
            {
                FileName = "RECENTLY_ADDED_MAPS",
                Name = "Recently added maps",
                Description = "The most recently downloaded maps.",
                CoverImage = "data:image/png;base64,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",
                DynamicPlaylistConfiguration = new DynamicPlaylistConfiguration
                {
                    MapPool = MapPool.Standard,
                    SortOperations = new()
                    {
                        new SortOperation
                        {
                            Field = nameof(DynamicPlaylistMap.AddedDateTime),
                            Direction = SortDirection.Descending
                        }
                    },
                    MapCount = 20
                }
            },
            new EditDynamicPlaylistModel
            {
                FileName = "IMPROVEMENT_MAPS",
                Name = "Improvement maps",
                Description = "Maps recommended to play for maximum pp gain.",
                CoverImage = "data:image/png;base64,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",
                DynamicPlaylistConfiguration = new DynamicPlaylistConfiguration
                {
                    MapPool = MapPool.Improvement,
                    FilterOperations = new()
                    {
                        new FilterOperation
                        {
                            Field = nameof(DynamicPlaylistMap.Hidden),
                            Operator = FilterOperator.Equals,
                            Value = false.ToString()
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(DynamicPlaylistMap.ScoreEstimate)}.{nameof(DynamicPlaylistScoreEstimate.Accuracy)}",
                            Operator = FilterOperator.GreaterThanOrEqual,
                            Value = 80.ToString()
                        }
                    },
                    SortOperations = new()
                    {
                        new SortOperation
                        {
                            Field = $"{nameof(DynamicPlaylistMap.ScoreEstimate)}.{nameof(DynamicPlaylistScoreEstimate.PPIncrease)}",
                            Direction = SortDirection.Descending
                        }
                    },
                    MapCount = 20
                }
            }
        };
    }
}