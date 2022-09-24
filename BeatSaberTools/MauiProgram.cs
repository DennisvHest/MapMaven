using BeatSaberTools.Infrastructure;
using BeatSaberTools.Services;
using MudBlazor;
using MudBlazor.Services;

namespace BeatSaberTools;

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

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddMudServices(config =>
		{
			config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
		});

		builder.Services.AddBeatSaberTools<BeatSaberToolFileService>();

#if WINDOWS
        builder.Services.AddTransient<IFolderPicker, Platforms.Windows.FolderPicker>();
#endif

        return builder.Build();
	}
}
