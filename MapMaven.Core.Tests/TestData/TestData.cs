using Dapper;
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
    }
}
