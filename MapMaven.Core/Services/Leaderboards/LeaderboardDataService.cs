﻿using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Reactive.Subjects;
using System.Text.Json;

namespace MapMaven.Core.Services.Leaderboards
{
    public class LeaderboardDataService : ILeaderboardDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IApplicationEventService _applicationEventService;
        private readonly ILogger<LeaderboardDataService> _logger;
        private readonly IFileSystem _fileSystem;

        private readonly BehaviorSubject<LeaderboardData?> _leaderboardData = new(null);

        public IObservable<LeaderboardData?> LeaderboardData => _leaderboardData;

        public static string LeaderboardDataPath => Path.Join(BeatSaberFileService.AppDataCacheLocation, "leaderboard-data.json");

        public LeaderboardDataService(IHttpClientFactory httpClientFactory, IApplicationEventService applicationEventService, ILogger<LeaderboardDataService> logger, IFileSystem fileSystem)
        {
            _httpClientFactory = httpClientFactory;
            _applicationEventService = applicationEventService;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public async Task LoadLeaderboardDataAsync()
        {
            try
            {
                var leaderboardData = await GetLeaderboardDataAsync();

                _leaderboardData.OnNext(leaderboardData);
            }
            catch (Exception ex)
            {
                _applicationEventService.RaiseError(new()
                {
                    Exception = ex,
                    Message = "Failed to load leaderboard data."
                });
            }
        }

        public async Task<LeaderboardData?> GetLeaderboardDataAsync()
        {
            string leaderboardDataJson;

            try
            {
                var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

                leaderboardDataJson = await httpClient.GetStringAsync($"leaderboard-data.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load leaderboard data from server. Falling back to local cache.");

                leaderboardDataJson = await _fileSystem.File.ReadAllTextAsync(LeaderboardDataPath);
            }

            if (!_fileSystem.Directory.Exists(BeatSaberFileService.AppDataCacheLocation))
                _fileSystem.Directory.CreateDirectory(BeatSaberFileService.AppDataCacheLocation);

            await _fileSystem.File.WriteAllTextAsync(LeaderboardDataPath, leaderboardDataJson);

            return JsonSerializer.Deserialize<LeaderboardData>(leaderboardDataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
