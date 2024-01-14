using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using Velopack;
using Velopack.Sources;

namespace MapMaven.Services
{
    public class UpdateService
    {
        private readonly UpdateManager _updateManager;

        private readonly ILogger<App> _logger;

        private ReplaySubject<UpdateInfo> _availableUpdate = new(1);
        public IObservable<UpdateInfo> AvailableUpdate => _availableUpdate;

        public bool IsInstalled => _updateManager.IsInstalled;
        public string CurrentVersion => _updateManager.CurrentVersion?.ToString() ?? "0.0.0";

        public UpdateService(ILogger<App> logger)
        {
            _logger = logger;

            _updateManager = GetUpdateManager(_logger);
        }

        public static UpdateManager GetUpdateManager() => GetUpdateManager(null);

#if DEBUG
        private static UpdateManager GetUpdateManager(ILogger logger) => new(@"C:\Users\denni\Desktop\BSTools\releases-test", logger: logger);
#else
        private static UpdateManager GetUpdateManager(ILogger logger) => new(
            new GithubSource(
                repoUrl: "https://github.com/DennisvHest/MapMaven",
                accessToken: null,
                prerelease: false,
                logger: logger
            )
        );
#endif

        public async Task CheckForUpdates()
        {
            try
            {
                _logger?.LogInformation("Checking for updates...");

                if (!_updateManager.IsInstalled)
                {
                    _logger?.LogInformation("App is not an installed app. Skipping update check.");
                    return;
                }

                var newVersion = await _updateManager.CheckForUpdatesAsync();

                if (newVersion is null)
                {
                    _logger?.LogInformation("No new update found.");
                    return;
                }

                await _updateManager.DownloadUpdatesAsync(newVersion);

                _logger?.LogInformation($"Update {newVersion.TargetFullRelease.Version} installed from {newVersion.TargetFullRelease.BaseUrl}.");

                _availableUpdate.OnNext(newVersion);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during update.");
            }
        }

        public void ApplyUpdatesAndRestart() => _updateManager.ApplyUpdatesAndRestart();
    }
}
