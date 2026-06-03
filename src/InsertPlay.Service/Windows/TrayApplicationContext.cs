using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using InsertPlay.Core;
using Microsoft.Extensions.Hosting;

namespace InsertPlay.Service.Windows;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IHost _host;
    private readonly ICredentialStore _credentialStore;
    private readonly NotifyIcon _notifyIcon;
    private LogViewerForm? _logForm;

    public TrayApplicationContext(IHost host, ICredentialStore credentialStore)
    {
        _host            = host;
        _credentialStore = credentialStore;

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
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
        menu.Items.Add("Conta RetroAchievements...", null, (_, _) => ShowRaLogin());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowLogViewer()
    {
        if (_logForm is null || _logForm.IsDisposed)
        {
            _logForm = new LogViewerForm(_credentialStore);
            _logForm.Show();
        }
        else
        {
            _logForm.WindowState = FormWindowState.Normal;
            _logForm.BringToFront();
            _logForm.Activate();
        }
    }

    private void ShowRaLogin()
    {
        using var form = new RetroAchievementsLoginForm(_credentialStore);
        form.ShowDialog();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Application.Exit();
    }

    internal static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("InsertPlay.Service.Resources.icon.png")!;
        using var bmp = new Bitmap(stream);
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
