using Dapper;
using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Tests.TestData;
using MapMaven.Infrastructure;
using MapMaven.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.IO.Abstractions;
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

            MockScoreSaberClient(services);
            MockBeatLeaderClient(services);
        }

        private static void MockScoreSaberClient(IServiceCollection services)
        {
            var scoreSaberApiClientMock = new Mock<ScoreSaberApiClient>(MockBehavior.Strict);

            scoreSaberApiClientMock
                .Setup(x => x.ScoresAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Sort?>(), It.IsAny<int?>(), It.IsAny<bool?>()))
                .Returns(Task.FromResult(TestData.TestData.TestScoreSaberPlayerScores));

            services.RemoveAll<ScoreSaberApiClient>();
            services.AddSingleton(scoreSaberApiClientMock.Object);
        }

        private static void MockBeatLeaderClient(IServiceCollection services)
        {
            var beatLeaderApiClientMock = new Mock<BeatLeaderApiClient>(MockBehavior.Strict);

            beatLeaderApiClientMock
                .Setup(x => x.ScoresAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Order?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Requirements?>(), It.IsAny<ScoreFilterStatus>(), It.IsAny<LeaderboardContexts?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<float?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .Returns(Task.FromResult(TestData.TestData.TestBeatLeaderPlayerScores));

            services.RemoveAll<BeatLeaderApiClient>();
            services.AddSingleton(beatLeaderApiClientMock.Object);
        }

        protected override ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

        protected override IEnumerable<TestAppSettings> GetTestAppSettings() => Enumerable.Empty<TestAppSettings>();
    }
}
