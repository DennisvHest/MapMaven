using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models;
using MapMaven.Core.Services.Leaderboards;
using Moq;
using System.Reactive.Linq;
using Xunit.Abstractions;

namespace MapMaven.Core.Tests.General
{
    [CollectionDefinition("ExternalServiceErrorTests")]
    public class ExternalServiceErrorTests : CoreIntegrationTestCollection
    {
        public ExternalServiceErrorTests(ITestOutputHelper testOutputHelper, MapMavenTestBedFixture fixture) : base(testOutputHelper, fixture)
        {
            var scoreSaberApiClientMock = fixture.GetService<Mock<ScoreSaberApiClient>>(testOutputHelper)!;

            scoreSaberApiClientMock
                .SetupSequence(x => x.ScoresAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Sort?>(), It.IsAny<int?>(), It.IsAny<bool?>()))
                .ThrowsAsync(new Exception("Test exception")) // First call throws an exception
                .Returns(Task.FromResult(TestData.TestData.TestScoreSaberPlayerScores));

            var beatLeaderApiClientMock = fixture.GetService<Mock<BeatLeaderApiClient>>(testOutputHelper)!;

            beatLeaderApiClientMock
                .SetupSequence(x => x.ScoresAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Order?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Requirements?>(), It.IsAny<ScoreFilterStatus?>(), It.IsAny<LeaderboardContexts?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<float?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("Test exception")) // First call throws an exception
                .Returns(Task.FromResult(TestData.TestData.TestBeatLeaderPlayerScores));
        }

        [Fact]
        public async Task ScoreSaberScoreRetrieval_AfterError_RecoversAfterError()
        {
            await ApplicationSettingService.AddOrUpdateAsync(ScoreSaberService.PlayerIdSettingKey, "test123");
            var maps = await MapService.Maps.FirstAsync();

            Assert.NotEmpty(maps);

            var testMap = maps.First(m => m.Hash == "051709ED4264F353EA329FB8803780E45D3BF8E5");

            Assert.Empty(testMap.AllPlayerScores);

            await MapService.RefreshDataAsync(forceRefresh: true);

            maps = await MapService.Maps.FirstAsync();

            testMap = maps.First(m => m.Hash == "051709ED4264F353EA329FB8803780E45D3BF8E5");

            Assert.NotEmpty(testMap.AllPlayerScores);

            var score = testMap.AllPlayerScores.First();

            Assert.Equal(100, score.Score.BaseScore);
        }

        [Fact]
        public async Task BeatLeaderScoreRetrieval_AfterError_RecoversAfterError()
        {
            await ApplicationSettingService.AddOrUpdateAsync(BeatLeaderService.PlayerIdSettingKey, "test456");

            await LeaderboardService.SetActiveLeaderboardProviderAsync(LeaderboardProvider.BeatLeader);

            var maps = await MapService.Maps.FirstAsync();

            Assert.NotEmpty(maps);

            var testMap = maps.First(m => m.Hash == "051709ED4264F353EA329FB8803780E45D3BF8E5");

            Assert.Empty(testMap.AllPlayerScores);

            await MapService.RefreshDataAsync(forceRefresh: true);

            maps = await MapService.Maps.FirstAsync();

            testMap = maps.First(m => m.Hash == "051709ED4264F353EA329FB8803780E45D3BF8E5");

            Assert.NotEmpty(testMap.AllPlayerScores);

            var score = testMap.AllPlayerScores.First();

            Assert.Equal(200, score.Score.BaseScore);
        }
    }
}
