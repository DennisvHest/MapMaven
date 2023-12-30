using MapMaven.Core.Models.Data;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Tests.TestData;
using MapMaven.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reactive.Linq;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MapMaven.Core.Tests
{
    [CollectionDefinition("Map Maven DI")]
    public class SetupTest : TestBed<MapMavenTestBedFixture>, IAsyncLifetime
    {
        private readonly MapMavenContext _db;
        private readonly IApplicationSettingService _applicationSettingService;
        private IMapService _mapService;


        public SetupTest(ITestOutputHelper testOutputHelper, MapMavenTestBedFixture fixture) : base(testOutputHelper, fixture)
        {
            _db = _fixture.GetService<MapMavenContext>(_testOutputHelper)!;
            _applicationSettingService = _fixture.GetService<IApplicationSettingService>(_testOutputHelper)!;
        }

        public async Task InitializeAsync()
        {
            _db.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = "BeatSaberInstallLocation",
                StringValue = MapMavenMockFileSystem.MockFilesBasePath
            });

            await _db.SaveChangesAsync();

            _mapService = _fixture.GetService<IMapService>(_testOutputHelper)!;

            await _applicationSettingService.LoadAsync();
            await _mapService.RefreshDataAsync();
        }

        [Fact]
        public async Task Test1()
        {
            await _applicationSettingService.AddOrUpdateAsync(ScoreSaberService.PlayerIdSettingKey, "test123");

            var maps = await _mapService.Maps.FirstAsync();
        }

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
    }
}
