using MapMaven.Core.Models.Data;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Tests.TestData;
using MapMaven.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Linq;
using Xunit.Abstractions;

namespace MapMaven.Core.Tests
{
    [CollectionDefinition("Map Maven DI")]
    public class SetupTest : CoreIntegrationTestCollection
    {
        public SetupTest(ITestOutputHelper testOutputHelper, MapMavenTestBedFixture fixture) : base(testOutputHelper, fixture) { }

        [Fact]
        public async Task Test1()
        {
            await ApplicationSettingService.AddOrUpdateAsync(ScoreSaberService.PlayerIdSettingKey, "test123");
            await ApplicationSettingService.AddOrUpdateAsync(BeatLeaderService.PlayerIdSettingKey, "test123");

            var maps = await MapService.Maps.FirstAsync();
        }
    }
}
