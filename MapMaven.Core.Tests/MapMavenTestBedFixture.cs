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
        protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        {
            services.AddMapMaven(useStatefulServices: true);

            services.RemoveAll<DbContextOptions<MapMavenContext>>();
            services.AddDbContext<MapMavenContext>(options =>
                options.UseInMemoryDatabase("MapMavenTest")
            );

            SqlMapper.AddTypeHandler(new TimeSpanSqlMapper());

            services.AddSingleton<IFileSystem>(_ => MapMavenMockFileSystem.Get());

            //var scoreSaberApiClientMock = new Mock<ScoreSaberApiClient>(MockBehavior.Strict);

            //scoreSaberApiClientMock
            //    .Setup(x => x.ScoresAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Sort?>(), It.IsAny<int?>(), It.IsAny<bool?>()))
            //    .re

            //services.RemoveAll<ScoreSaberApiClient>();
            //services.AddSingleton<ScoreSaberApiClient>(() => );
        }

        protected override ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

        protected override IEnumerable<TestAppSettings> GetTestAppSettings() => Enumerable.Empty<TestAppSettings>();
    }
}
