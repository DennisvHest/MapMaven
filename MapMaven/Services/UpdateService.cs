using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace MapMaven.Services
{
    public class UpdateService
    {
        private readonly ILogger<App> _logger;

        public UpdateService(ILogger<App> logger)
        {
            _logger = logger;
        }

        public static UpdateManager GetUpdateManager() => GetUpdateManager(null);

        private static UpdateManager GetUpdateManager(ILogger logger) => new(
            new GithubSource(
                repoUrl: "https://github.com/DennisvHest/MapMaven",
                accessToken: null,
                prerelease: false,
                logger: logger
            )
        );

        public async Task CheckForUpdates()
        {
            var updateManager = GetUpdateManager(_logger);

            try
            {
                _logger?.LogInformation("Checking for updates...");

                if (!updateManager.IsInstalled)
                {
                    _logger?.LogInformation("App is not an installed app. Skipping update check.");
                    return;
                }

                var newVersion = await updateManager.CheckForUpdatesAsync();

                if (newVersion is null)
                {
                    _logger?.LogInformation("No new update found.");
                    return;
                }

                await updateManager.DownloadUpdatesAsync(newVersion);

                _logger?.LogInformation($"Update {newVersion.TargetFullRelease.Version} installed from {newVersion.TargetFullRelease.BaseUrl}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during update.");
            }
        }
    }
}
