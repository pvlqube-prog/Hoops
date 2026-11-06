using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;

namespace Qube2K23Launcher;

public sealed class MainForm : Form
{
    private const string SteamAppId = "1644960";
    private const string SteamLaunchUri = "steam://run/" + SteamAppId;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly Label _steamStatus = new();
    private readonly Label _communityStatus = new();
    private readonly TextBox _serverUrl = new();
    private readonly Button _launchButton = new();
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QUBE", "2K23Launcher", "settings.json");

    public MainForm()
    {
        Text = "QUBE 2K23 Launcher";
        MinimumSize = new Size(760, 520);
        Size = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 16, 25);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        BuildInterface();
        LoadSettings();
        Shown += async (_, _) => await RefreshStatusAsync();
    }

    private void BuildInterface()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(28, 20, 28, 12), BackColor = Color.FromArgb(19, 24, 37) };
        var title = new Label { Text = "QUBE 2K23", AutoSize = true, Font = new Font("Segoe UI", 25F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(24, 15) };
        var subtitle = new Label { Text = "COMMUNITY LAUNCHER  •  WINDOWS", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(145, 157, 180), Location = new Point(28, 65) };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        Controls.Add(header);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = BackColor };
        Controls.Add(content);

        var intro = new Label { Text = "Launch through Steam", AutoSize = true, Font = new Font("Segoe UI", 17F, FontStyle.Bold), Location = new Point(28, 24) };
        var explanation = new Label
        {
            Text = "This launcher uses Steam to start NBA 2K23. Steam must be installed and the account must own the game.",
            AutoSize = false, Size = new Size(780, 48), Location = new Point(30, 65),
            ForeColor = Color.FromArgb(165, 175, 195)
        };
        content.Controls.Add(intro);
        content.Controls.Add(explanation);

        _launchButton.Text = "LAUNCH NBA 2K23";
        _launchButton.Location = new Point(30, 132);
        _launchButton.Size = new Size(260, 58);
        _launchButton.FlatStyle = FlatStyle.Flat;
        _launchButton.FlatAppearance.BorderSize = 0;
        _launchButton.BackColor = Color.FromArgb(119, 75, 255);
        _launchButton.ForeColor = Color.White;
        _launchButton.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _launchButton.Click += (_, _) => LaunchThroughSteam();
        content.Controls.Add(_launchButton);

        var statusPanel = new Panel { Location = new Point(30, 220), Size = new Size(780, 116), BackColor = Color.FromArgb(23, 29, 44), Padding = new Padding(18) };
        _steamStatus.Text = "Steam: checking…";
        _steamStatus.AutoSize = true;
        _steamStatus.Location = new Point(18, 18);
        _communityStatus.Text = "Community services: not checked";
        _communityStatus.AutoSize = true;
        _communityStatus.Location = new Point(18, 52);
        statusPanel.Controls.Add(_steamStatus);
        statusPanel.Controls.Add(_communityStatus);
        content.Controls.Add(statusPanel);

        var serverLabel = new Label { Text = "Community status endpoint (optional)", AutoSize = true, Location = new Point(30, 365), ForeColor = Color.FromArgb(185, 194, 211) };
        content.Controls.Add(serverLabel);
        _serverUrl.Location = new Point(30, 395);
        _serverUrl.Size = new Size(590, 32);
        _serverUrl.BackColor = Color.FromArgb(23, 29, 44);
        _serverUrl.ForeColor = Color.White;
        _serverUrl.BorderStyle = BorderStyle.FixedSingle;
        content.Controls.Add(_serverUrl);

        var checkButton = new Button { Text = "CHECK", Location = new Point(635, 393), Size = new Size(110, 34), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(42, 51, 72), ForeColor = Color.White };
        checkButton.Click += async (_, _) => { SaveSettings(); await RefreshStatusAsync(); };
        content.Controls.Add(checkButton);

        var note = new Label
        {
            Text = "Note: NBA 2K23's official online services have been retired. This endpoint only reports the status of independently hosted QUBE community services; it does not restore 2K multiplayer.",
            AutoSize = false, Size = new Size(780, 55), Location = new Point(30, 450),
            ForeColor = Color.FromArgb(140, 151, 174), Font = new Font("Segoe UI", 8.5F)
        };
        content.Controls.Add(note);
    }

    private void LaunchThroughSteam()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = SteamLaunchUri, UseShellExecute = true });
            _steamStatus.Text = "Steam: launch request sent";
        }
        catch (Exception ex)
        {
            _steamStatus.Text = "Steam: unable to open Steam";
            MessageBox.Show("Install Steam and sign in to an account that owns NBA 2K23.\n\n" + ex.Message,
                "Steam launch unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task RefreshStatusAsync()
    {
        _steamStatus.Text = IsSteamInstalled() ? "Steam: detected" : "Steam: not detected";
        var endpoint = _serverUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _communityStatus.Text = "Community services: add a status endpoint to check";
            return;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            _communityStatus.Text = "Community services: enter a valid HTTP(S) URL";
            return;
        }

        try
        {
            using var response = await Http.GetAsync(uri);
            _communityStatus.Text = response.IsSuccessStatusCode
                ? "Community services: ONLINE"
                : $"Community services: response {(int)response.StatusCode}";
        }
        catch (Exception)
        {
            _communityStatus.Text = "Community services: OFFLINE / unreachable";
        }
    }

    private static bool IsSteamInstalled()
    {
        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\steam.exe",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Steam\steam.exe"
        };
        return candidates.Any(File.Exists);
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            if (doc.RootElement.TryGetProperty("statusEndpoint", out var endpoint))
                _serverUrl.Text = endpoint.GetString() ?? "";
        }
        catch { /* Ignore invalid local settings and keep defaults. */ }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var json = JsonSerializer.Serialize(new { statusEndpoint = _serverUrl.Text.Trim() }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not save launcher settings: " + ex.Message, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
