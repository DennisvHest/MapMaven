using Hardcodet.Wpf.TaskbarNotification.Interop;
using BeatSaberTools.Services;

namespace BeatSaberTools.Platforms.Windows;
public class TrayService : ITrayService
{
    WindowsTrayIcon tray;

    public Action ClickHandler { get; set; }

    public void Initialize()
    {
        tray = new WindowsTrayIcon("Platforms/Windows/trayicon.ico");
        tray.LeftClick = () =>
        {
            WindowExtensions.BringToFront();
            ClickHandler?.Invoke();
        };
    }
}
