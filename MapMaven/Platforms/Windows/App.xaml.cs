// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Hardcodet.Wpf.TaskbarNotification.Interop;
using MapMaven.Platforms.Windows;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace MapMaven.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    private readonly SingleInstanceDesktopApp _singleInstanceApp;

    private LaunchActivatedEventArgs _launchEventArgs = null;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
	{
		InitializeComponent();

        _singleInstanceApp = new SingleInstanceDesktopApp("MAP-MAVEN");
        _singleInstanceApp.Launched += OnSingleInstanceLaunched;
    }

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _launchEventArgs = args;
        _singleInstanceApp.Launch(args.Arguments);
    }

    private void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            base.OnLaunched(_launchEventArgs);
        }
        else
        {
            Platforms.Windows.WindowExtensions.BringToFront();
        }
    }
}

