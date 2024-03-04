using MapMaven.Services;
using Squirrel;
using System.Diagnostics;
using System.Reflection;

namespace MapMaven.Utility
{
    public static class ApplicationUtils
    {
        public static void RestartApplication()
        {
            var updateManager = UpdateService.GetUpdateManager();

            if (updateManager.IsInstalledApp)
            {
                UpdateManager.RestartApp();
            }
            else
            {
                string applicationPath = Assembly.GetEntryAssembly().Location
                    .Replace(".dll", ".exe");

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Process.Start(applicationPath);
                };

                Environment.Exit(0);
            }
        }
    }
}
