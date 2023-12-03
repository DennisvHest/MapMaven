using Azure.Storage.Blobs;
using ComposableAsync;
using MapMaven.Core.ApiClients.BeatLeader;
using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.Models.Data.Leaderboards.BeatLeader;
using MapMaven.Core.Models.Data.RankedMaps;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace MapMaven.Functions.Services
{
    public class BeatLeaderRankedMapService : RankedMapService<BeatLeaderFullRankedMapInfoItem>
    {
        private readonly BeatLeaderApiClient _beatLeaderApiClient;

        protected override string _leaderBoardProviderName => "beatleader";

        public BeatLeaderRankedMapService(BeatLeaderApiClient beatLeaderApiClient, ILogger<BeatLeaderRankedMapService> logger, BlobContainerClient mapMavenBlobContainerClient, BeatSaverApiClient beatSaverApiClient)
            : base(logger, beatSaverApiClient, mapMavenBlobContainerClient)
        {
            _beatLeaderApiClient = beatLeaderApiClient;
        }

        protected override async Task<IEnumerable<BeatLeaderFullRankedMapInfoItem>> GetAllRankedMapsAsync(CancellationToken cancellationToken = default)
        {
            var rankedMaps = new List<LeaderboardInfoResponse>();

            int totalMaps;
            double itemsPerPage;
            var page = 1;
            _logger.LogInformation("Fetching ranked maps from BeatLeader...");

            var rateLimit = TimeLimiter.GetFromMaxCountByInterval(18, TimeSpan.FromSeconds(10));

            do
            {
                await rateLimit;

                var rankedMapsCollection = await _beatLeaderApiClient.LeaderboardsAsync(
                    page: page,
                    count: 100,
                    sortBy: (Core.ApiClients.BeatLeader.SortBy)Core.Models.Data.Leaderboards.BeatLeader.SortBy.Timestamp,
                    order: (Core.ApiClients.BeatLeader.Order)Core.Models.Data.Leaderboards.BeatLeader.Order.Desc,
                    search: string.Empty,
                    type: (Core.ApiClients.BeatLeader.Type)LeaderboardType.Ranked,
                    mode: default,
                    mapType: default,
                    allTypes: default,
                    mapRequirements: default,
                    allRequirements: default,
                    mytype: default,
                    stars_from: default,
                    stars_to: default,
                    accrating_from: default,
                    accrating_to: default,
                    passrating_from: default,
                    passrating_to: default,
                    techrating_from: default,
                    techrating_to: default,
                    date_from: default,
                    date_to: default,
                    cancellationToken: cancellationToken
                );

                itemsPerPage = rankedMapsCollection.Metadata.ItemsPerPage;
                totalMaps = rankedMapsCollection.Metadata.Total;

                rankedMaps.AddRange(rankedMapsCollection.Data);

                _logger.LogInformation($"Fetched page {page} out of {Math.Ceiling(totalMaps / itemsPerPage)} ({rankedMaps.Count}/{totalMaps} maps).");

                page++;
            }
            while ((page - 1) * itemsPerPage < totalMaps);

            return rankedMaps
                .GroupBy(m => m.Song.Hash.ToUpper())
                .Select(leaderboards => new BeatLeaderFullRankedMapInfoItem
                {
                    SongHash = leaderboards.Key,
                    Leaderboards = leaderboards
                });
        }

        protected override RankedMapInfoItem MapRankedMapInfoItem(FullRankedMapInfoItem fullRankedMapInfoItem)
        {
            return new RankedMapInfoItem((BeatLeaderFullRankedMapInfoItem)fullRankedMapInfoItem);
        }
    }
}
