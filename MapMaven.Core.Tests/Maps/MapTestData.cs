﻿using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Models;

namespace MapMaven.Core.Tests.Maps
{
    public static class MapTestData
    {
        public static readonly IEnumerable<Map> Maps = new Map[]
        {
            new Map
            {
                Id = "1",
                Name = "Test Map",
                SongAuthorName = "Camellia",
                RankedMap = new RankedMap
                {
                    Stars = 1,
                    PP = 20
                },
                PlayerScore = new PlayerScore
                {
                    Score = new Score
                    {
                        TimeSet = new DateTime(2023, 1, 12)
                    }
                }
            },
            new Map
            {
                Id = "2",
                Name = "Other test map",
                SongAuthorName = "Camellia",
                RankedMap = new RankedMap
                {
                    Stars = 10,
                    PP = 100
                }
            },
            new Map
            {
                Id = "3",
                Name = "sleepparalysis//////////////",
                SongAuthorName = "Test song author",
                RankedMap = new RankedMap
                {
                    Stars = 5.2,
                    PP = 43.2
                },
                PlayerScore = new PlayerScore
                {
                    Score = new Score
                    {
                        TimeSet = new DateTime(2022, 3, 25)
                    }
                }
            }
        };
    }
}