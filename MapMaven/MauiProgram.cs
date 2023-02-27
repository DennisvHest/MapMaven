using MapMaven.Core.Services;
using MapMaven.Infrastructure;
using MapMaven.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Squirrel;
using System.Reflection;
using System.Diagnostics;
using ShellLink;
using Microsoft.Extensions.Hosting;
using MapMaven.Utility;

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

        builder.Services.AddSingleton<IHostedService, Worker.Worker>();
        builder.Services.AddSingleton<HostedServiceExecutor>();

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

        Task.Run(() => ExecuteStartupProcedures(mauiApp));

        var trayService = mauiApp.Services.GetService<ITrayService>();
        trayService.Initialize();

        StartupSetup.Initialize(mauiApp.Services);

        return mauiApp;
    }

    private static async Task ExecuteStartupProcedures(MauiApp mauiApp)
    {
        var logger = mauiApp.Services.GetService<ILogger<App>>();

        try
        {
            using var updateManager = new GithubUpdateManager("https://github.com/DennisvHest/MapMaven", prerelease: true);

            AddShortcutToStartupFolder(logger, updateManager.IsInstalledApp);

            await CheckForUpdates(logger, updateManager);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during startup procedures.");
        }
    }

    private static void AddShortcutToStartupFolder(ILogger<App> logger, bool isInstalledApp)
    {
        try
        {
            logger?.LogInformation($"Adding shortcut to startup folder...");

            string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\MapMaven.lnk";
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            logger?.LogInformation($"Current directory for shortcut to .exe: {currentDirectory}");

            var exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            if (isInstalledApp)
            {
                exePath = Path.GetFullPath(Path.Combine(exePath, @"..\"));
            }

            exePath = Path.Combine(exePath, "MapMaven.exe");

            logger?.LogInformation($"TargetPath for shortcut to .exe: {exePath}");

            Shortcut.CreateShortcut(
                path: exePath,
                args: "startupLaunch",
                workdir: currentDirectory,
                iconpath: currentDirectory + @"\Assets\appicon.ico",
                iconindex: 0
            ).WriteToFile(shortcutAddress);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Cannot create shortcut in startup folder.");
        }
    }

    private static async Task CheckForUpdates(ILogger<App> logger, GithubUpdateManager updateManager)
    {
        try
        {
            logger?.LogInformation("Checking for updates...");

            if (!updateManager.IsInstalledApp)
            {
                logger?.LogInformation("App is not an installed app. Skipping update check.");
                return;
            }

            var update = await updateManager.UpdateApp();

            if (update == null)
            {
                logger?.LogInformation("No new update found.");
            }
            else
            {
                logger?.LogInformation($"Update {update.Version} installed from {update.BaseUrl}.");
            }
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
