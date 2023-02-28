using MapMaven.Services;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MapMaven.Platforms.Windows;
public class TrayService : ITrayService
{
    private readonly MapService _mapService;

    public TrayService(MapService mapService)
    {
        _mapService = mapService;
    }

    public void Initialize()
    {
        var contextMenu = new ContextMenuStrip();

        contextMenu.Items.Add(
            text: "Open Map Maven",
            image: null,
            onClick: (_, _) => BringToFront()
        );

        contextMenu.Items.Add(
            text: "Exit",
            image: null,
            onClick: (_, _) => Process.GetCurrentProcess().Kill()
        );

        var notifyIcon = new NotifyIcon();

        notifyIcon.Icon = new Icon("Platforms/Windows/trayicon.ico");
        notifyIcon.ContextMenuStrip = contextMenu;
        notifyIcon.Text = "Map Maven";
        notifyIcon.Visible = true;

        notifyIcon.DoubleClick += (_, _) => BringToFront();
    }

    public void BringToFront()
    {
        WindowExtensions.BringToFront();
    }
}
