using Microsoft.Extensions.Logging;
using Squirrel;

namespace MapMaven.Services
{
    public class UpdateService
    {
        private readonly ILogger<App> _logger;

        public UpdateService(ILogger<App> logger)
        {
            _logger = logger;
        }

        public static UpdateManager GetUpdateManager() => new GithubUpdateManager("https://github.com/DennisvHest/MapMaven", prerelease: true);

        public async Task CheckForUpdates()
        {
            using var updateManager = GetUpdateManager();

            try
            {
                _logger?.LogInformation("Checking for updates...");

                if (!updateManager.IsInstalledApp)
                {
                    _logger?.LogInformation("App is not an installed app. Skipping update check.");
                    return;
                }

                var update = await updateManager.UpdateApp();

                if (update == null)
                {
                    _logger?.LogInformation("No new update found.");
                }
                else
                {
                    _logger?.LogInformation($"Update {update.Version} installed from {update.BaseUrl}.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during update.");
            }
        }
    }
}
