using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.RankedMaps;
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
                Hash = "1",
                Name = "Test Map",
                SongAuthorName = "Camellia",
                Difficulty = new RankedMapDifficultyInfo
                {
                    Stars = 1
                },
                HighestPlayerScore = new PlayerScore
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
                Hash = "2",
                Name = "Other test map",
                SongAuthorName = "Camellia",
                Difficulty = new RankedMapDifficultyInfo
                {
                    Stars = 10
                }
            },
            new Map
            {
                Id = "3",
                Hash = "3",
                Name = "sleepparalysis//////////////",
                SongAuthorName = "Test song author",
                Difficulty = new RankedMapDifficultyInfo
                {
                    Stars = 5.2
                },
                HighestPlayerScore = new PlayerScore
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
