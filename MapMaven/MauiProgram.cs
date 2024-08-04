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
using System.Reflection;
using System.Diagnostics;
using ShellLink;
using Microsoft.Extensions.Hosting;
using MapMaven.Utility;
using Serilog.Events;
using MapMaven.Services.Workers;
using MapMaven.Services.Playlists;
using MudExtensions.Services;
using MapMaven.Core.Models.AdvancedSearch;
using System.Windows;

namespace MapMaven;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            MauiApp mauiApp = null;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
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

            builder.Services.AddMudExtensions();

            builder.Services.AddMapMaven(useStatefulServices: true);

            builder.Services.AddSingleton<UpdateService>();
            builder.Services.AddSingleton<IHostedService, UpdateWorker>();
            builder.Services.AddSingleton<IHostedService, LivePlaylistWorker>();
            builder.Services.AddSingleton<IHostedService, MemoryCleaningWorker>();
            builder.Services.AddSingleton<HostedServiceExecutor>();
            builder.Services.AddSingleton<PlaylistCoverService>();

            builder.Services.AddSingleton<AdvancedSearchConfiguration>();

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

                        winuiAppWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                        winuiAppWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                        winuiAppWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                        winuiAppWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);

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
        catch
        {
            var result = MessageBox.Show(
                messageBoxText: "Map Maven could not start due to a critical error. Click 'Ok' to reset Map Maven and restart. Note: this will remove all settings and cache.",
                caption: "Map Maven",
                button: MessageBoxButton.OKCancel,
                icon: MessageBoxImage.Error
            );

            if (result == MessageBoxResult.OK)
            {
                var applicationFilesService = new ApplicationFilesService(new System.IO.Abstractions.FileSystem());

                applicationFilesService.DeleteApplicationFiles();
                ApplicationUtils.RestartApplication();

                return null;
            }
            else
            {
                throw;
            }
        }
    }

    private static async Task ExecuteStartupProcedures(MauiApp mauiApp)
    {
        var logger = mauiApp.Services.GetService<ILogger<App>>();

        try
        {
            using var updateManager = UpdateService.GetUpdateManager();

            AddShortcutToStartupFolder(logger, updateManager.IsInstalledApp);
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
}
