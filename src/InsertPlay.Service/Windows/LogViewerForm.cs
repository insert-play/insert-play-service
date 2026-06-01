using System.Drawing;
using System.Windows.Forms;
using Serilog.Events;

namespace InsertPlay.Service.Windows;

internal sealed class LogViewerForm : Form
{
    private readonly RichTextBox _logBox;

    public LogViewerForm()
    {
        Text = "InsertPlay — Logs";
        Size = new Size(960, 620);
        MinimumSize = new Size(640, 400);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);

        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.LightGray,
            Font = new Font("Consolas", 9.5f),
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
        };

        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = Color.FromArgb(45, 45, 48),
        };

        var btnClear = MakeButton("Limpar", 8);
        btnClear.Click += (_, _) => _logBox.Clear();

        var btnOpenFolder = MakeButton("Abrir Pasta de Logs", 96, width: 150);
        btnOpenFolder.Click += (_, _) => OpenLogFolder();

        toolbar.Controls.AddRange(new Control[] { btnClear, btnOpenFolder });
        Controls.AddRange(new Control[] { _logBox, toolbar });

        // Populate with already-buffered entries
        foreach (var entry in InMemoryLogSink.Instance.GetSnapshot())
            AppendEntry(entry);

        InMemoryLogSink.Instance.EntryAdded += OnEntryAdded;
        FormClosed += (_, _) => InMemoryLogSink.Instance.EntryAdded -= OnEntryAdded;
    }

    // Clicking X hides the window instead of closing it; the app keeps running.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    // Minimising also hides to tray.
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
            Hide();
    }

    private void OnEntryAdded(object? sender, LogEntry entry)
    {
        if (InvokeRequired)
            BeginInvoke(() => AppendEntry(entry));
        else
            AppendEntry(entry);
    }

    private void AppendEntry(LogEntry entry)
    {
        var levelTag = entry.Level switch
        {
            LogEventLevel.Verbose     => "[VRB]",
            LogEventLevel.Debug       => "[DBG]",
            LogEventLevel.Information => "[INF]",
            LogEventLevel.Warning     => "[WRN]",
            LogEventLevel.Error       => "[ERR]",
            LogEventLevel.Fatal       => "[FTL]",
            _                         => "[???]",
        };

        var color = entry.Level switch
        {
            LogEventLevel.Verbose     => Color.DimGray,
            LogEventLevel.Debug       => Color.Gray,
            LogEventLevel.Information => Color.LightGray,
            LogEventLevel.Warning     => Color.Gold,
            LogEventLevel.Error       => Color.Tomato,
            LogEventLevel.Fatal       => Color.OrangeRed,
            _                         => Color.LightGray,
        };

        var text = $"{entry.Timestamp:HH:mm:ss} {levelTag} {entry.Message}";
        if (entry.Exception is not null)
            text += $"\n  {entry.Exception}";
        text += "\n";

        var start = _logBox.TextLength;
        _logBox.AppendText(text);
        _logBox.Select(start, text.Length);
        _logBox.SelectionColor = color;
        _logBox.SelectionLength = 0;
        _logBox.ScrollToCaret();
    }

    private static void OpenLogFolder()
    {
        var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsPath);
        System.Diagnostics.Process.Start("explorer.exe", logsPath);
    }

    private static Button MakeButton(string text, int left, int width = 90) => new()
    {
        Text = text,
        Width = width,
        Height = 26,
        Left = left,
        Top = 6,
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.White,
        BackColor = Color.FromArgb(63, 63, 70),
    };
}
