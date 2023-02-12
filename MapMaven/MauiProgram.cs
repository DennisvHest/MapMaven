using MapMaven.Core.Services;
using MapMaven.Infrastructure;
using MapMaven.Services;
using Microsoft.Maui.LifecycleEvents;
using MudBlazor;
using MudBlazor.Services;
using Serilog;

namespace MapMaven;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
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

            lifecycle.AddWindows(windows => windows.OnWindowCreated((del) =>
            {
                del.ExtendsContentIntoTitleBar = false;
            }));
#endif
        });

#if WINDOWS
        builder.Services.AddTransient<IFolderPicker, Platforms.Windows.FolderPicker>();
        builder.Services.AddSingleton<ITrayService, MapMaven.Platforms.Windows.TrayService>();
#endif

        var mauiApp = builder.Build();

        StartupSetup.Initialize(mauiApp.Services);

        return mauiApp;
    }
}
