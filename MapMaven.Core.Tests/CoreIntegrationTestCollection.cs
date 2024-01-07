using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Tests.TestData;
using MapMaven.Infrastructure.Data;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MapMaven.Core.Tests
{
    public abstract class CoreIntegrationTestCollection : TestBed<MapMavenTestBedFixture>
    {
        protected readonly IApplicationSettingService ApplicationSettingService;
        protected readonly IMapService MapService;
        protected readonly ILeaderboardService LeaderboardService;

        public CoreIntegrationTestCollection(ITestOutputHelper testOutputHelper, MapMavenTestBedFixture fixture) : base(testOutputHelper, fixture)
        {
            ApplicationSettingService = _fixture.GetService<IApplicationSettingService>(_testOutputHelper)!;
            MapService = _fixture.GetService<IMapService>(_testOutputHelper)!;
            LeaderboardService = _fixture.GetService<ILeaderboardService>(_testOutputHelper)!;
        }
    }
}
