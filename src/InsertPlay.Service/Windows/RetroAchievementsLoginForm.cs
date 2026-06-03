using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using InsertPlay.Core;
using InsertPlay.Core.Models;

namespace InsertPlay.Service.Windows;

internal sealed class RetroAchievementsLoginForm : Form
{
    private static readonly HttpClient s_httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly ICredentialStore _credentialStore;
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly Button _btnDisconnect;
    private readonly Button _btnSave;
    private readonly Label _lblStatus;

    public RetroAchievementsLoginForm(ICredentialStore credentialStore)
    {
        _credentialStore = credentialStore;

        Text             = "InsertPlay — RetroAchievements";
        Icon             = TrayApplicationContext.LoadIcon();
        Size             = new Size(560, 340);
        MinimumSize      = new Size(500, 310);
        StartPosition    = FormStartPosition.CenterParent;
        BackColor        = Color.FromArgb(30, 30, 30);
        FormBorderStyle  = FormBorderStyle.Sizable;
        MaximizeBox      = false;
        MinimizeBox      = false;

        const int labelH = 16;
        const int inputH = 24;
        const int left   = 16;
        var width        = ClientSize.Width - (left * 2);

        var lblUsername = MakeLabel("Nome de usuário:", left, 16, width, labelH);
        lblUsername.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _txtUsername = new TextBox
        {
            Left      = left,
            Top       = 36,
            Width     = width,
            Height    = inputH,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.LightGray,
            BorderStyle = BorderStyle.FixedSingle,
            Font      = new Font("Consolas", 9.5f),
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var lblPassword = MakeLabel("Senha da conta:", left, 72, width, labelH);
        lblPassword.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _txtPassword = new TextBox
        {
            Left         = left,
            Top          = 92,
            Width        = width,
            Height       = inputH,
            BackColor    = Color.FromArgb(45, 45, 48),
            ForeColor    = Color.LightGray,
            BorderStyle  = BorderStyle.FixedSingle,
            Font         = new Font("Consolas", 9.5f),
            PasswordChar = '\u25cf', // ●
            Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var info = MakeLabel(
            "Use a senha da conta RetroAchievements. API key de /settings nao funciona no RetroArch/PCSX2.",
            left,
            128,
            width,
            32);
        info.Font = new Font("Segoe UI", 8.5f);
        info.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _lblStatus = MakeLabel(string.Empty, left, 162, width, 16);
        _lblStatus.Font = new Font("Segoe UI", 8.5f);
        _lblStatus.ForeColor = Color.Gold;
        _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var linkToken = new LinkLabel
        {
            Text      = "Abrir RetroAchievements",
            Left      = left,
            Top       = 184,
            Width     = width,
            Height    = labelH,
            BackColor = Color.Transparent,
            ForeColor = Color.CornflowerBlue,
            LinkColor = Color.CornflowerBlue,
            ActiveLinkColor = Color.SkyBlue,
            Font      = new Font("Segoe UI", 8.5f),
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        linkToken.LinkClicked += (_, _) =>
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://retroachievements.org/settings") { UseShellExecute = true });

        // Bottom toolbar
        var toolbar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 52,
            BackColor = Color.FromArgb(45, 45, 48),
        };

        _btnDisconnect = MakeButton("Desconectar", 8);
        _btnDisconnect.Click += OnDisconnect;
        _btnDisconnect.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        var btnCancel = MakeButton("Cancelar", 0);
        btnCancel.Click += (_, _) => Close();
        btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnSave = MakeButton("Salvar", 0);
        _btnSave.Click      += OnSave;
        _btnSave.BackColor  = Color.FromArgb(0, 122, 204);
        _btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        void LayoutToolbarButtons()
        {
            _btnDisconnect.Left = 8;
            _btnDisconnect.Top = 12;

            _btnSave.Left = toolbar.ClientSize.Width - _btnSave.Width - 8;
            _btnSave.Top = 12;

            btnCancel.Left = _btnSave.Left - btnCancel.Width - 8;
            btnCancel.Top = 12;
        }

        toolbar.Controls.AddRange(new Control[] { _btnDisconnect, btnCancel, _btnSave });
        toolbar.Resize += (_, _) => LayoutToolbarButtons();
        Shown += (_, _) => LayoutToolbarButtons();

        Controls.AddRange(new Control[] { lblUsername, _txtUsername, lblPassword, _txtPassword, info, _lblStatus, linkToken, toolbar });

        // Pre-fill from saved credentials
        var existing = _credentialStore.Load();
        if (existing is not null)
        {
            _txtUsername.Text = existing.Username;
            _txtPassword.Text = string.IsNullOrWhiteSpace(existing.Password)
                ? existing.ApiToken
                : existing.Password;
        }
        UpdateDisconnectButton();
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show(
                "Preencha o nome de usuario e a senha da conta.",
                "InsertPlay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _btnSave.Enabled = false;
        _lblStatus.ForeColor = Color.Gold;
        _lblStatus.Text = "Validando credenciais...";

        var (isValid, validationMessage) = await ValidateCredentialsAsync(username, password);
        if (!isValid)
        {
            _lblStatus.ForeColor = Color.Tomato;
            _lblStatus.Text = validationMessage;
            _btnSave.Enabled = true;
            return;
        }

        _credentialStore.Save(new RetroAchievementsCredentials
        {
            Username = username,
            Password = password,
        });

        _lblStatus.ForeColor = Color.LightGreen;
        _lblStatus.Text = "Credenciais validas.";

        UpdateDisconnectButton();
        Close();
    }

    private void OnDisconnect(object? sender, EventArgs e)
    {
        _credentialStore.Clear();
        _txtUsername.Clear();
        _txtPassword.Clear();
        UpdateDisconnectButton();
    }

    private void UpdateDisconnectButton()
    {
        _btnDisconnect.Enabled = _credentialStore.Load() is not null;
    }

    private static Label MakeLabel(string text, int left, int top, int width, int height) => new()
    {
        Text      = text,
        Left      = left,
        Top       = top,
        Width     = width,
        Height    = height,
        ForeColor = Color.LightGray,
        BackColor = Color.Transparent,
        Font      = new Font("Segoe UI", 9f),
    };

    private static Button MakeButton(string text, int left, int width = 86) => new()
    {
        Text      = text,
        Left      = left,
        Top       = 10,
        Width     = width,
        Height    = 26,
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.White,
        BackColor = Color.FromArgb(63, 63, 70),
    };

    private static async Task<(bool IsValid, string Message)> ValidateCredentialsAsync(
        string username, string password)
    {
        const string url = "https://retroachievements.org/dorequest.php";
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["r"] = "login2",
            ["u"] = username,
            ["p"] = password,
        });

        try
        {
            using var response = await s_httpClient.PostAsync(url, body);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, "Falha ao validar agora. Tente novamente em instantes.");

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (TryGetBoolean(root, "Success", out var success) && success)
                return (true, string.Empty);

            if (TryGetString(root, "Token", out var token) && !string.IsNullOrWhiteSpace(token))
                return (true, string.Empty);

            if (TryGetString(root, "Error", out var error) && !string.IsNullOrWhiteSpace(error))
                return (false, error);

            if (TryGetString(root, "Message", out var message) && !string.IsNullOrWhiteSpace(message))
                return (false, message);

            return (false, "Usuario ou senha incorretos.");
        }
        catch
        {
            return (false, "Nao foi possivel validar no RetroAchievements agora.");
        }
    }

    private static bool TryGetBoolean(JsonElement root, string property, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(property, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            value = number != 0;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString();
            if (bool.TryParse(text, out var parsedBool))
            {
                value = parsedBool;
                return true;
            }
            if (int.TryParse(text, out var parsedInt))
            {
                value = parsedInt != 0;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetString(JsonElement root, string property, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(property, out var element))
            return false;

        if (element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString();
        return true;
    }
}
