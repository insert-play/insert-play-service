using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace InsertPlay.Service.Windows;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IHost _host;
    private readonly NotifyIcon _notifyIcon;
    private LogViewerForm? _logForm;

    public TrayApplicationContext(IHost host)
    {
        _host = host;

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "InsertPlay",
            Visible = true,
            ContextMenuStrip = BuildContextMenu(),
        };

        _notifyIcon.DoubleClick += (_, _) => ShowLogViewer();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Ver Logs", null, (_, _) => ShowLogViewer());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowLogViewer()
    {
        if (_logForm is null || _logForm.IsDisposed)
        {
            _logForm = new LogViewerForm();
            _logForm.Show();
        }
        else
        {
            _logForm.WindowState = FormWindowState.Normal;
            _logForm.BringToFront();
            _logForm.Activate();
        }
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Application.Exit();
    }

    /// <summary>Creates a simple blue play-button icon for the system tray.</summary>
    private static Icon CreateIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(0, 120, 215)), 0, 0, 16, 16);
            g.FillPolygon(Brushes.White, new Point[]
            {
                new(5, 3),
                new(13, 8),
                new(5, 13),
            });
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _logForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
