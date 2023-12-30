using Dapper;
using MapMaven.Core.Tests.TestData;
using MapMaven.Infrastructure;
using MapMaven.Infrastructure.Data;
using MapMaven.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MapMaven.Core.Tests
{
    public class MapMavenTestBedFixture : TestBedFixture
    {
        public const string MockFilesBasePath = @"f:\bslegacylauncher\installed versions\beat saber 1.29.1";

        protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        {
            services.AddMapMaven(useStatefulServices: true);

            services.RemoveAll<DbContextOptions<MapMavenContext>>();
            services.AddDbContext<MapMavenContext>(options =>
                options.UseInMemoryDatabase("MapMavenTest")
            );

            SqlMapper.AddTypeHandler(new TimeSpanSqlMapper());

            services.AddSingleton<IFileSystem>(_ => MockFileSystem());

            //var scoreSaberApiClientMock = new Mock<ScoreSaberApiClient>(MockBehavior.Strict);

            //scoreSaberApiClientMock
            //    .Setup(x => x.ScoresAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Sort?>(), It.IsAny<int?>(), It.IsAny<bool?>()))
            //    .re

            //services.RemoveAll<ScoreSaberApiClient>();
            //services.AddSingleton<ScoreSaberApiClient>(() => );
        }

        protected override ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

        protected override IEnumerable<TestAppSettings> GetTestAppSettings() => Enumerable.Empty<TestAppSettings>();

        private static MockFileSystem MockFileSystem()
        {
            var songHashData = GetMockSongHashData(TestData.TestData.TestMaps.Value);

            var mockFiles = new Dictionary<string, MockFileData>
            {
                { $"{MockFilesBasePath}/UserData/SongCore/SongHashData.dat", new MockFileData(songHashData) },
            };

            AddMockMapInfo(mockFiles, TestData.TestData.TestMaps.Value);

            return new MockFileSystem(mockFiles);
        }

        private static string GetMockSongHashData(IEnumerable<MapInfo> maps)
        {
            var mapsJson = maps.Select(m =>
            {
                var path = m.DirectoryPath
                    .Replace("f:\\bslegacylauncher\\installed versions\\beat saber 1.29.1\\", string.Empty)
                    .Replace(@"\", @"\\");

                return $$"""
                    ".\\{{path}}": {
                        "directoryHash": 1,
                        "songHash": "{{m.Hash ?? Guid.NewGuid().ToString()}}"
                    }
                """;
            });

            return $$"""
            {
                {{string.Join(",", mapsJson)}}
            }
            """;
        }

        private static void AddMockMapInfo(Dictionary<string, MockFileData> mockFiles, IEnumerable<MapInfo> maps)
        {
            foreach (var map in maps)
            {
                AddMockMapInfo(mockFiles, map);
            }
        }

        private static void AddMockMapInfo(Dictionary<string, MockFileData> mapInfoDictionary, MapInfo mapInfo)
        {
            var mapInfoJson = JsonSerializer.Serialize(mapInfo);

            mapInfoDictionary.Add($"{mapInfo.DirectoryPath}/", new MockDirectoryData());
            mapInfoDictionary.Add($"{mapInfo.DirectoryPath}/Info.dat", new MockFileData(mapInfoJson));
        }
    }
}
