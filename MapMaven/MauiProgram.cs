using MapMaven.Core.Services;
using MapMaven.Infrastructure;
using MapMaven.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Squirrel;

namespace MapMaven;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiApp mauiApp = null;

        SquirrelAwareApp.HandleEvents(
            onInitialInstall: OnAppInstall,
            onAppUninstall: OnAppUninstall,
            onEveryRun: OnAppRun
        );

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Join(BeatSaberFileService.AppDataLocation, "logs", "app-logs", "app-log-.txt"),
                rollingInterval: RollingInterval.Day
            ).CreateLogger();

        builder.Services.AddLogging(loggingBuilder =>
          loggingBuilder.AddSerilog(dispose: true));

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
        });

        builder.Services.AddMapMaven();

        builder.ConfigureLifecycleEvents(lifecycle =>
        {
#if WINDOWS

            lifecycle.AddWindows(windows =>
            {
                windows.OnWindowCreated((window) =>
                {
                    var trayService = mauiApp.Services.GetService<ITrayService>();

                    window.ExtendsContentIntoTitleBar = false;

                    IntPtr nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                    AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);

                    winuiAppWindow.Closing += (w, e) =>
                    {
                        e.Cancel = true;
                        Platforms.Windows.WindowExtensions.MinimizeToTray();
                    };

                    Platforms.Windows.WindowExtensions.Hwnd = nativeWindowHandle;
                });
            });
#endif
        });

#if WINDOWS
        builder.Services.AddTransient<IFolderPicker, Platforms.Windows.FolderPicker>();
        builder.Services.AddSingleton<ITrayService, Platforms.Windows.TrayService>();
#endif

        mauiApp = builder.Build();

        var trayService = mauiApp.Services.GetService<ITrayService>();

        trayService.Initialize();

        Task.Run(() => CheckForUpdates(mauiApp.Services.GetService<ILogger<App>>()));

        StartupSetup.Initialize(mauiApp.Services);

        return mauiApp;
    }

    private static async Task CheckForUpdates(ILogger<App> logger)
    {
        logger?.LogInformation("Checking for updates...");

        try
        {
            using var updateManager = new GithubUpdateManager("https://github.com/DennisvHest/MapMaven", prerelease: true);

            if (!updateManager.IsInstalledApp)
            {
                logger?.LogInformation("App is not an installed app. Skipping update check.");
                return;
            }

            await updateManager.UpdateApp();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during update.");
        }
    }

    private static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
    }

    private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
    }

    private static void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun)
    {
        tools.SetProcessAppUserModelId();
    }
}
