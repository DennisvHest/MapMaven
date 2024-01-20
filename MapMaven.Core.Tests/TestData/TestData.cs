using Dapper;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Models.Data;
using Microsoft.Data.Sqlite;

namespace MapMaven.Core.Tests.TestData
{
    public static class TestData
    {
        public static Lazy<IEnumerable<MapInfo>> TestMaps = new(() =>
        {
            using var db = new SqliteConnection("Data Source=./TestData/TestData.db");

            return db.Query<MapInfo>("SELECT * FROM MapInfos");
        });

        public static ApiClients.ScoreSaber.PlayerScoreCollection TestScoreSaberPlayerScores = new()
        {
            PlayerScores = new List<ApiClients.ScoreSaber.PlayerScore>
            {
                new()
                {
                    Leaderboard = new()
                    {
                        Id = 1,
                        SongHash = "051709ED4264F353EA329FB8803780E45D3BF8E5",
                        Difficulty = new()
                        {
                            DifficultyRaw = "ExpertPlus_SoloStandard"
                        }
                    },
                    Score = new()
                    {
                        Id = 1,
                        BaseScore = 100,
                    }
                }
            },
            Metadata = new()
            {
                ItemsPerPage = 100,
                Page = 1,
                Total = 100
            }
        };

        public static ApiClients.BeatLeader.ScoreResponseWithMyScoreResponseWithMetadata TestBeatLeaderPlayerScores = new()
        {
            Data = new List<ApiClients.BeatLeader.ScoreResponseWithMyScore>
            {
                new()
                {
                    Leaderboard = new()
                    {
                        Id = "1",
                        Song = new()
                        {
                            Hash = "051709ED4264F353EA329FB8803780E45D3BF8E5",
                        },
                        Difficulty = new()
                        {
                            DifficultyName = "ExpertPlus"
                        }
                    },
                    BaseScore = 200
                }
            },
            Metadata = new()
            {
                ItemsPerPage = 100,
                Page = 1,
                Total = 100
            }
        };

        public static RankedMapInfo TestScoreSaberRankedMaps = new()
        {
            RankedMaps = new()
        };

        public static RankedMapInfo TestBeatLeaderRankedMaps = new()
        {
            RankedMaps = new()
        };
    }
}
