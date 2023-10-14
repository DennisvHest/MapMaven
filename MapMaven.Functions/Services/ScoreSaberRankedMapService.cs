using Azure.Storage.Blobs;
using ComposableAsync;
using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Functions.Models.ScoreSaber;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace MapMaven.Functions.Services
{
    public class ScoreSaberRankedMapService : RankedMapService<ScoreSaberFullRankedMapInfoItem>
    {
        private readonly ScoreSaberApiClient _scoreSaberApiClient;

        protected override string _leaderBoardProviderName => "scoresaber";

        public ScoreSaberRankedMapService(ScoreSaberApiClient scoreSaberApiClient, ILogger<ScoreSaberRankedMapService> logger, BlobContainerClient mapMavenBlobContainerClient, BeatSaverApiClient beatSaverApiClient)
            : base(logger, beatSaverApiClient, mapMavenBlobContainerClient)
        {
            _scoreSaberApiClient = scoreSaberApiClient;
        }

        protected override async Task<IEnumerable<ScoreSaberFullRankedMapInfoItem>> GetAllRankedMapsAsync(CancellationToken cancellationToken = default)
        {
            var rankedMaps = new List<LeaderboardInfo>();

            int totalMaps;
            double itemsPerPage;
            var page = 1;
            _logger.LogInformation("Fetching ranked maps from ScoreSaber...");

            var rateLimit = TimeLimiter.GetFromMaxCountByInterval(380, TimeSpan.FromMinutes(1));

            do
            {
                await rateLimit;

                var rankedMapsCollection = await _scoreSaberApiClient.LeaderboardsAsync(
                    search: string.Empty,
                    verified: default,
                    ranked: true,
                    qualified: default,
                    loved: default,
                    minStar: default,
                    maxStar: default,
                    category: default,
                    sort: LeaderboardSort.DateDescending,
                    unique: default,
                    page: page,
                    withMetadata: true,
                    cancellationToken: cancellationToken
                );

                itemsPerPage = rankedMapsCollection.Metadata.ItemsPerPage;
                totalMaps = rankedMapsCollection.Metadata.Total;

                rankedMaps.AddRange(rankedMapsCollection.Leaderboards);

                _logger.LogInformation($"Fetched page {page} out of {Math.Ceiling(totalMaps / itemsPerPage)} ({rankedMaps.Count}/{totalMaps} maps).");

                page++;
            }
            while ((page - 1) * itemsPerPage < totalMaps);

            return rankedMaps
                .GroupBy(m => m.SongHash.ToUpper())
                .Select(leaderboards => new ScoreSaberFullRankedMapInfoItem
                {
                    SongHash = leaderboards.Key,
                    Leaderboards = leaderboards
                });
        }

        protected override RankedMapInfoItem MapRankedMapInfoItem(FullRankedMapInfoItem fullRankedMapInfoItem)
        {
            return new RankedMapInfoItem((ScoreSaberFullRankedMapInfoItem)fullRankedMapInfoItem);
        }
    }
}
