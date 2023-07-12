using ComposableAsync;
using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Utilities.Scoresaber;
using MapMaven.RankedMapUpdater.Models.ScoreSaber;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace MapMaven.RankedMapUpdater.Services
{
    public class RankedMapService
    {
        private readonly ILogger<RankedMapService> _logger;

        private readonly ScoreSaberApiClient _scoreSaberApiClient;

        public RankedMapService(ScoreSaberApiClient scoreSaberApiClient, ILogger<RankedMapService> logger)
        {
            _scoreSaberApiClient = scoreSaberApiClient;
            _logger = logger;
        }

        public async Task UpdateRankedMaps(CancellationToken? cancellationToken = null)
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
                    cancellationToken: cancellationToken ?? CancellationToken.None
                );

                itemsPerPage = rankedMapsCollection.Metadata.ItemsPerPage;
                totalMaps = rankedMapsCollection.Metadata.Total;

                rankedMaps.AddRange(rankedMapsCollection.Leaderboards);

                _logger.LogInformation($"Fetched page {page} out of {Math.Ceiling(totalMaps / itemsPerPage)} ({rankedMaps.Count}/{totalMaps} maps).");

                page++;
            }
            while ((page - 1) * itemsPerPage < totalMaps);            
        }
    }
}
