﻿using Dapper;
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
            PlayerScores = new List<ApiClients.ScoreSaber.PlayerScore>(),
            Metadata = new()
            {
                ItemsPerPage = 100,
                Page = 1,
                Total = 100
            }
        };
    }
}
