using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using MudBlazor.Services;
using BeatSaberTools.Services;
using BeatSaber.SongHashing;

namespace BeatSaberTools
{
    public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.RegisterBlazorMauiWebView()
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});

			builder.Services.AddBlazorWebView();
			builder.Services.AddMudServices();

			builder.Services.AddSingleton<IBeatmapHasher, Hasher>();

			builder.Services.AddSingleton<BeatSaberDataService>();
			builder.Services.AddSingleton<MapService>();
			builder.Services.AddSingleton<SongPlayerService>();
			builder.Services.AddSingleton<PlaylistService>();

			return builder.Build();
		}
	}
}