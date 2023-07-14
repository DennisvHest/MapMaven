using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComposableAsync;
using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Utilities.Scoresaber;
using MapMaven.RankedMapUpdater.Models.ScoreSaber;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RateLimiter;
using System.Text;

namespace MapMaven.RankedMapUpdater.Services
{
    public class RankedMapService
    {
        private readonly ILogger<RankedMapService> _logger;

        private readonly ScoreSaberApiClient _scoreSaberApiClient;

        private readonly BlobContainerClient _mapMavenBlobContainerClient;

        public RankedMapService(ScoreSaberApiClient scoreSaberApiClient, ILogger<RankedMapService> logger, BlobContainerClient mapMavenBlobContainerClient)
        {
            _scoreSaberApiClient = scoreSaberApiClient;
            _logger = logger;
            _mapMavenBlobContainerClient = mapMavenBlobContainerClient;
        }

        public async Task UpdateRankedMaps(CancellationToken cancellationToken = default)
        {
            var rankedMaps = new List<RankedMapInfoItem>();

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

                var rankedMapInfo = rankedMapsCollection.Leaderboards.Select(l => new RankedMapInfoItem { Leaderboard = l });

                rankedMaps.AddRange(rankedMapInfo);

                _logger.LogInformation($"Fetched page {page} out of {Math.Ceiling(totalMaps / itemsPerPage)} ({rankedMaps.Count}/{totalMaps} maps).");

                page++;
            }
            while ((page - 1) * itemsPerPage < totalMaps);

            var rankedMapInfoJson = JsonConvert.SerializeObject(new RankedMapInfo { RankedMaps = rankedMaps });

            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(rankedMapInfoJson));

            var rankedMapsBlob = _mapMavenBlobContainerClient.GetBlobClient("scoresaber/ranked-maps.json");

            await rankedMapsBlob.UploadAsync(jsonStream, new BlobUploadOptions
            {
                HttpHeaders = new() { ContentType = "application/json" }
            });
        }
    }
}
