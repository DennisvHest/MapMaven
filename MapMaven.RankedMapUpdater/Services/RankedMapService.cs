﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComposableAsync;
using MapMaven.Core.ApiClients.BeatSaver;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data.RankedMaps;
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
        private readonly BlobClient _rankedMapsBlob;

        public RankedMapService(ScoreSaberApiClient scoreSaberApiClient, ILogger<RankedMapService> logger, BlobContainerClient mapMavenBlobContainerClient, BeatSaverApiClient beatSaverApiClient)
        {
            _scoreSaberApiClient = scoreSaberApiClient;
            _logger = logger;
            _mapMavenBlobContainerClient = mapMavenBlobContainerClient;
            _beatSaverApiClient = beatSaverApiClient;

            _rankedMapsBlob = _mapMavenBlobContainerClient.GetBlobClient("scoresaber/ranked-maps.json");
        }

        public async Task UpdateRankedMapsAsync(DateTime lastRunDate, CancellationToken cancellationToken = default)
        {
            var rankedMapInfo = await GetExistingRankedMapInfoAsync();
            var rankedMaps = await GetAllRankedMapsAsync(cancellationToken);

            var rankedMapsBySongHash = rankedMaps.GroupBy(m => m.SongHash);

            // Join the existing ranked maps with the new ranked maps to preserve the map details
            rankedMapInfo.RankedMaps = rankedMapsBySongHash.GroupJoin(rankedMapInfo.RankedMaps, m => m.Key, m => m.SongHash, (updatedLeaderboardInfo, existingRankedMapInfo) =>
            {
                var rankedMapInfoItem = new FullRankedMapInfoItem
                {
                    SongHash = updatedLeaderboardInfo.Key,
                    Leaderboards = updatedLeaderboardInfo
                };

                if (existingRankedMapInfo.Any())
                {
                    var existingMap = existingRankedMapInfo.First();
                    rankedMapInfoItem.MapDetail = existingMap.MapDetail;
                }

                return rankedMapInfoItem;
            }).ToList();

            await UpdateMapDetailForExistingMapInfoAsync(lastRunDate, rankedMapInfo, cancellationToken);

            var mapInfoWithoutDetails = rankedMapInfo.RankedMaps
                .Where(m => m.MapDetail is null)
                .ToList();

            await GetMapDetailForMapInfoAsync(mapInfoWithoutDetails, cancellationToken);

            await UploadRankedMapInfoAsync(rankedMapInfo);
        }

        private async Task UpdateMapDetailForExistingMapInfoAsync(DateTime lastRunDate, FullRankedMapInfo rankedMapInfo, CancellationToken cancellationToken)
        {
            var updatedMaps = await GetUpdatedMapsAsync(lastRunDate, cancellationToken);

            var mapInfoWithUpdatedDetails = rankedMapInfo.RankedMaps
                .Where(m => m.MapDetail is not null)
                .Join(updatedMaps, m => m.MapDetail.Id, m => m.Id, (mapInfo, mapDetail) => (mapInfo, mapDetail))
                .ToList();

            _logger.LogInformation("{mapInfoWithUpdatedDetailsCount} Ranked maps have updated details.", mapInfoWithUpdatedDetails.Count());

            foreach (var (mapInfo, mapDetail) in mapInfoWithUpdatedDetails)
            {
                mapInfo.MapDetail = mapDetail;
            }
        }

        private async Task GetMapDetailForMapInfoAsync(IEnumerable<FullRankedMapInfoItem> mapInfoWithoutDetails, CancellationToken cancellationToken)
        {
            var count = mapInfoWithoutDetails.Count();

            _logger.LogInformation("{count} Maps do not yet have any map details from Beat Saver.", count);

            var rateLimit = TimeLimiter.GetFromMaxCountByInterval(8, TimeSpan.FromSeconds(1));

            foreach (var (mapInfo, index) in mapInfoWithoutDetails.Select((mapInfo, index) => (mapInfo, index)))
            {
                await rateLimit;

                var mapDetail = await _beatSaverApiClient.HashAsync(mapInfo.SongHash, cancellationToken);

                mapInfo.MapDetail = mapDetail;

                var doneCount = index + 1;

                _logger.LogInformation("({doneCount} / {count}) Fetched map details for map with hash: {SongHash}...", doneCount, count, mapInfo.SongHash);
            }
        }

        private async Task<FullRankedMapInfo> GetExistingRankedMapInfoAsync()
        {
            if (!await _rankedMapsBlob.ExistsAsync())
            {
                _logger.LogInformation("No existing ranked maps JSON found.");
                return new();
            }

            using var stream = await _rankedMapsBlob.OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = JsonSerializer.CreateDefault();

            return serializer.Deserialize<FullRankedMapInfo>(jsonReader);
        }

        private async Task<IEnumerable<LeaderboardInfo>> GetAllRankedMapsAsync(CancellationToken cancellationToken = default)
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

            return rankedMaps;
        }

        private async Task<IEnumerable<MapDetail>> GetUpdatedMapsAsync(DateTime lastRunDate, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching latest maps updated after: {lastRunDate}...", lastRunDate);

            var response = await _beatSaverApiClient.LatestAsync(
                after: lastRunDate.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                automapper: default,
                before: default,
                sort: Core.ApiClients.BeatSaver.Sort.UPDATED,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Found {updatedCount} maps with updated details.", response.Docs.Count);

            return response.Docs;
        }

        private async Task UploadRankedMapInfoAsync(FullRankedMapInfo rankedMapInfo)
        {
            var rankedMapInfoJson = JsonConvert.SerializeObject(rankedMapInfo);

            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(rankedMapInfoJson));

            await _rankedMapsBlob.UploadAsync(jsonStream, new BlobUploadOptions
            {
                HttpHeaders = new() { ContentType = "application/json" }
            });
        }
    }
}
