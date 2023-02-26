using MapMaven.Services;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MapMaven.Platforms.Windows;
public class TrayService : ITrayService
{
    public void Initialize()
    {
        var contextMenu = new ContextMenuStrip();

        contextMenu.Items.Add(
            text: "Open Map Maven",
            image: null,
            onClick: (_, _) => WindowExtensions.BringToFront()
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

        notifyIcon.DoubleClick += (_, _) => WindowExtensions.BringToFront();
    }
}
