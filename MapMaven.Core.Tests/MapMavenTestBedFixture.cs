using MapMaven.Infrastructure;
using MapMaven.Infrastructure.Data;
using MapMaven.Models.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MapMaven.Core.Tests
{
    public class MapMavenTestBedFixture : TestBedFixture
    {
        private SqliteConnection _dbConnection;

        public const string MockFilesBasePath = "Beat Saber";

        protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        {
            services.AddMapMaven(useStatefulServices: true);

            //_dbConnection = new SqliteConnection("DataSource=:memory:");
            //_dbConnection.Open();

            services.RemoveAll<DbContextOptions<MapMavenContext>>();
            services.AddDbContext<MapMavenContext>(options =>
                //options.UseSqlite(_dbConnection)
                options.UseInMemoryDatabase("MapMavenTest")
            );

            services.AddSingleton<IFileSystem>(_ => MockFileSystem());
        }

        protected override async ValueTask DisposeAsyncCore() => await _dbConnection.DisposeAsync();

        protected override IEnumerable<TestAppSettings> GetTestAppSettings() => Enumerable.Empty<TestAppSettings>();

        private static MockFileSystem MockFileSystem()
        {
            var maps = new TestMap[]
            {
                new()
                {
                    MapInfo = new() { SongName = "KillerToy", SongAuthorName = "Camellia", DirectoryPath = "1A466 ([Queue] Camellia - KillerToy - Jabob)" },
                    Hash = "1"
                },
                new()
                {
                    MapInfo = new() { SongName = "Nacreous Snowmelt", SongAuthorName = "Camellia", DirectoryPath = "6F01 (Camellia - Nacreous Snowmelt - Dack)" },
                    Hash = "2"
                },
                new()
                {
                    MapInfo = new() { SongName = "Come Alive", SongAuthorName = "Pendulum", DirectoryPath = "188b1 (Come Alive - Fatalution)" },
                    Hash = "test123"
                },
                new()
                {
                    MapInfo = new() { SongName = "Halcyon", SongAuthorName = "xi", DirectoryPath = "235b (Halcyon - splake)" },
                    HasSongCoreHash = false
                },
                new()
                {
                    MapInfo = new() { SongName = "Duplicate ID", SongAuthorName = "TEST 1", DirectoryPath = "ABCD1 (Duplicate ID 1)" },
                    CreatedDateTime = new DateTime(2023, 02, 10, 0, 0, 0)
                },
                new()
                {
                    MapInfo = new() { SongName = "Duplicate ID", SongAuthorName = "TEST 2", DirectoryPath = "ABCD1 (Duplicate ID 2)" },
                    CreatedDateTime = new DateTime(2023, 02, 11, 12, 30, 0)
                }
            };

            var songHashData = GetMockSongHashData(maps);

            var mockFiles = new Dictionary<string, MockFileData>
            {
                { $"{MockFilesBasePath}/UserData/SongCore/SongHashData.dat", new MockFileData(songHashData) },
            };

            AddMockMapInfo(mockFiles, maps);

            return new MockFileSystem(mockFiles);
        }

        private static string GetMockSongHashData(IEnumerable<TestMap> maps)
        {
            var mapsJson = maps.Where(m => m.HasSongCoreHash).Select(m => $$"""
                ".\\Beat Saber_Data\\CustomLevels\\{{m.MapInfo.DirectoryPath}}": {
                    "directoryHash": 1,
                    "songHash": "{{m.Hash ?? Guid.NewGuid().ToString()}}"
                }
            """);

            return $$"""
            {
                {{string.Join(",", mapsJson)}}
            }
            """;
        }

        private static void AddMockMapInfo(Dictionary<string, MockFileData> mockFiles, TestMap[] maps)
        {
            foreach (var map in maps)
            {
                AddMockMapInfo(mockFiles, map);
            }
        }

        private static void AddMockMapInfo(Dictionary<string, MockFileData> mapInfoDictionary, TestMap mapInfo)
        {
            var mapInfoJson =
            $$"""
            {
              "_songName": "{{mapInfo.MapInfo.SongName}}",
              "_songAuthorName": "{{mapInfo.MapInfo.SongAuthorName}}",
              "_coverImageFilename": "cover.jpg",
              "_levelAuthorName": "test",
              "_songFilename": "song.egg"
            }
            """;

            var mockDirectoryData = new MockDirectoryData { CreationTime = mapInfo.CreatedDateTime };

            mapInfoDictionary.Add($"{MockFilesBasePath}/Beat Saber_Data/CustomLevels/{mapInfo.MapInfo.DirectoryPath}/", mockDirectoryData);
            mapInfoDictionary.Add($"{MockFilesBasePath}/Beat Saber_Data/CustomLevels/{mapInfo.MapInfo.DirectoryPath}/Info.dat", new MockFileData(mapInfoJson));
        }

        private class TestMap
        {
            public MapInfo MapInfo { get; set; }
            public string? Hash { get; set; }
            public bool HasSongCoreHash { get; set; } = true;
            public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.Now;
        }
    }
}
