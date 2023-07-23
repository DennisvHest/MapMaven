using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComposableAsync;
using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data;
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
        private readonly BeatSaverApiClient _beatSaverApiClient;

        private readonly BlobContainerClient _mapMavenBlobContainerClient;

        public RankedMapService(ScoreSaberApiClient scoreSaberApiClient, ILogger<RankedMapService> logger, BlobContainerClient mapMavenBlobContainerClient, BeatSaverApiClient beatSaverApiClient)
        {
            _scoreSaberApiClient = scoreSaberApiClient;
            _logger = logger;
            _mapMavenBlobContainerClient = mapMavenBlobContainerClient;
            _beatSaverApiClient = beatSaverApiClient;
        }

        public async Task UpdateRankedMaps(DateTime lastRunDate, CancellationToken cancellationToken = default)
        {
            var rankedMapsBlob = _mapMavenBlobContainerClient.GetBlobClient("scoresaber/ranked-maps.json");

            var existingRankedMapInfo = await GetExistingRankedMapInfo(rankedMapsBlob);
            var updatedMaps = await GetUpdatedMaps(lastRunDate, cancellationToken);

            var rankedMaps = await GetAllRankedMaps(cancellationToken);

            var rankedMapInfoJson = JsonConvert.SerializeObject(new RankedMapInfo { RankedMaps = rankedMaps });

            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(rankedMapInfoJson));

            await rankedMapsBlob.UploadAsync(jsonStream, new BlobUploadOptions
            {
                HttpHeaders = new() { ContentType = "application/json" }
            });
        }

        private static async Task<RankedMapInfo?> GetExistingRankedMapInfo(BlobClient rankedMapsBlob)
        {
            if (!await rankedMapsBlob.ExistsAsync())
                return null;

            using var stream = await rankedMapsBlob.OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = JsonSerializer.CreateDefault();

            return serializer.Deserialize<RankedMapInfo>(jsonReader);
        }

        private async Task<IEnumerable<RankedMapInfoItem>> GetAllRankedMaps(CancellationToken cancellationToken = default)
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

            return rankedMaps;
        }

        private async Task<IEnumerable<MapDetail>> GetUpdatedMaps(DateTime lastRunDate, CancellationToken cancellationToken = default)
        {
            var response = await _beatSaverApiClient.LatestAsync(
                after: lastRunDate.AddDays(-2).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                automapper: default,
                before: default,
                sort: Core.ApiClients.BeatSaver.Sort.UPDATED,
                cancellationToken: cancellationToken
            );

            return response.Docs;
        }
    }
}
