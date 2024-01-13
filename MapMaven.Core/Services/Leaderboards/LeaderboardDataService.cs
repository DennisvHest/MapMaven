using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using System.Text.Json;

namespace MapMaven.Core.Services.Leaderboards
{
    public class LeaderboardDataService : ILeaderboardDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IApplicationEventService _applicationEventService;
        private readonly ILogger<LeaderboardDataService> _logger;

        private readonly CachedValue<LeaderboardData?> _leaderboardData;

        public IObservable<LeaderboardData?> LeaderboardData => _leaderboardData.ValueObservable;

        public static string LeaderboardDataPath => Path.Join(BeatSaberFileService.AppDataCacheLocation, "leaderboard-data.json");

        public LeaderboardDataService(IHttpClientFactory httpClientFactory, IApplicationEventService applicationEventService, ILogger<LeaderboardDataService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _applicationEventService = applicationEventService;
            _logger = logger;

            _leaderboardData = new(GetLeaderboardDataAsync, TimeSpan.FromHours(6));
        }

        public void ReloadLeaderboardData()
        {
            _leaderboardData.UpdateValue();
        }

        public async Task<LeaderboardData?> GetLeaderboardDataAsync()
        {
            try
            {
                string leaderboardDataJson;

                _logger.LogInformation("Loading leaderboard data from server.");

                try
                {
                    var httpClient = _httpClientFactory.CreateClient("MapMavenFiles");

                    leaderboardDataJson = await httpClient.GetStringAsync($"leaderboard-data.json");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load leaderboard data from server. Falling back to local cache.");

                    leaderboardDataJson = await File.ReadAllTextAsync(LeaderboardDataPath);
                }

                if (!Directory.Exists(BeatSaberFileService.AppDataCacheLocation))
                    Directory.CreateDirectory(BeatSaberFileService.AppDataCacheLocation);

                await File.WriteAllTextAsync(LeaderboardDataPath, leaderboardDataJson);

                return JsonSerializer.Deserialize<LeaderboardData>(leaderboardDataJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _applicationEventService.RaiseError(new()
                {
                    Exception = ex,
                    Message = "Failed to load leaderboard data."
                });

                return _leaderboardData.Value;
            }
        }
    }
}
