using MapMaven.Core.Models;
using MapMaven.Core.Services.Leaderboards;
using System.Reactive.Linq;
using Xunit.Abstractions;

namespace MapMaven.Core.Tests.Leaderboards
{
    [CollectionDefinition("LeaderboardSwitchTests")]
    public class LeaderboardSwitchTests : CoreIntegrationTestCollection
    {
        public LeaderboardSwitchTests(ITestOutputHelper testOutputHelper, MapMavenTestBedFixture fixture) : base(testOutputHelper, fixture) { }

        [Fact]
        public async Task SwitchingActiveLeaderboard_With2LeaderboardsActive_CorrectlySwitches()
        {
            await ApplicationSettingService.AddOrUpdateAsync(ScoreSaberService.PlayerIdSettingKey, "test123");
            await ApplicationSettingService.AddOrUpdateAsync(BeatLeaderService.PlayerIdSettingKey, "test456");

            // Set active leaderboard to ScoreSaber
            await LeaderboardService.SetActiveLeaderboardProviderAsync(LeaderboardProvider.ScoreSaber);
            await AssertActiveLeaderboardSetting(LeaderboardProvider.ScoreSaber);

            // Set active leaderboard to BeatLeader
            await LeaderboardService.SetActiveLeaderboardProviderAsync(LeaderboardProvider.BeatLeader);
            await AssertActiveLeaderboardSetting(LeaderboardProvider.BeatLeader);

            // Remove BeatLeader player ID, which should switch the active leaderboard to ScoreSaber
            await ApplicationSettingService.AddOrUpdateAsync<string?>(BeatLeaderService.PlayerIdSettingKey, null);
            await AssertActiveLeaderboardSetting(LeaderboardProvider.ScoreSaber);

            // Remove ScoreSaber player ID. Previous active leaderboard stays.
            await ApplicationSettingService.AddOrUpdateAsync<string?>(ScoreSaberService.PlayerIdSettingKey, null);
            await AssertActiveLeaderboardSetting(LeaderboardProvider.ScoreSaber);
        }

        private async Task AssertActiveLeaderboardSetting(LeaderboardProvider leaderboardProvider)
        {
            var applicationSettings = await ApplicationSettingService.ApplicationSettings.FirstAsync();

            var activeLeaderboardSetting = applicationSettings["ActiveLeaderboardProvider"];

            Assert.NotNull(activeLeaderboardSetting?.StringValue);
            Assert.Equal(leaderboardProvider.ToString(), activeLeaderboardSetting.StringValue);
        }
    }
}
