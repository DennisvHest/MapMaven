using Microsoft.Extensions.Logging;
using Squirrel;
using Squirrel.Sources;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Services
{
    public class UpdateService
    {
        private readonly UpdateManager _updateManager;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger<App> _logger;

        private ReplaySubject<UpdateInfo> _availableUpdate = new(1);
        public IObservable<UpdateInfo> AvailableUpdate => _availableUpdate.Take(1);

        public bool IsInstalled => _updateManager.IsInstalledApp;
        public string CurrentVersion => _updateManager.CurrentlyInstalledVersion()?.ToString() ?? "0.0.0";

        public UpdateService(ILogger<App> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;

            _updateManager = GetUpdateManager();
            _httpClientFactory = httpClientFactory;
        }

#if DEBUG
        public static UpdateManager GetUpdateManager() => new(@"C:\Users\denni\Desktop\MapMaven\releases-test");
#else
        public static UpdateManager GetUpdateManager() => new(
            new GithubSource(
                repoUrl: "https://github.com/DennisvHest/MapMaven",
                accessToken: null,
                prerelease: false
            )
        );
#endif

        public async Task CheckForUpdates()
        {
            _availableUpdate.OnNext(null);

            try
            {
                _logger?.LogInformation("Checking for updates...");

                if (!_updateManager.IsInstalledApp)
                {
                    _logger?.LogInformation("App is not an installed app. Skipping update check.");
                    return;
                }

                var update = await _updateManager.CheckForUpdate();
                var newVersion = await _updateManager.UpdateApp();

                if (newVersion is null)
                {
                    _logger?.LogInformation("No new update found.");
                    return;
                }

                _logger?.LogInformation($"Update {newVersion?.Version} installed from {newVersion?.BaseUrl}.");

                _availableUpdate.OnNext(update);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during update.");
            }
        }

        public async Task<ReleaseInfo> GetLatestReleaseInfo()
        {
            var httpClient = _httpClientFactory.CreateClient("GithubApi");

            return await httpClient.GetFromJsonAsync<ReleaseInfo>("releases/latest");
        }
    }

    public class ReleaseInfo
    {
        public string Body { get; set; }
    }
}
