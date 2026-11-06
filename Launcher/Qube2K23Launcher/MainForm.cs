using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.Json;

namespace Qube2K23Launcher;

public sealed class MainForm : Form
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly Label _gamePath = new();
    private readonly Label _scanStatus = new();
    private readonly Label _serverStatus = new();
    private readonly TextBox _serverUrl = new();
    private readonly Button _playButton = new();
    private readonly Button _scanButton = new();
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "QUBE", "2K23Launcher", "settings.json");
    private string? _gameExe;
    private bool _scanning;

    public MainForm()
    {
        Text = "QUBE 2K23";
        MinimumSize = new Size(900, 680);
        Size = new Size(1180, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(9, 13, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;
        BuildInterface();
        LoadSettings();
        Shown += async (_, _) => await ScanComputerAsync();
    }

    private void BuildInterface()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = Color.FromArgb(8, 12, 22), Padding = new Padding(30, 18, 25, 12) };
        header.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(header.ClientRectangle, Color.FromArgb(27, 42, 75), Color.FromArgb(12, 17, 31), 0f);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
        };
        var brand = new Label { Text = "QUBE", AutoSize = true, Font = new Font("Segoe UI", 30F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(28, 13), BackColor = Color.Transparent };
        var tag = new Label { Text = "2K23  •  PC GAME LAUNCHER", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(125, 167, 255), Location = new Point(32, 65), BackColor = Color.Transparent };
        header.Controls.Add(brand); header.Controls.Add(tag); Controls.Add(header);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = Color.FromArgb(7, 10, 18) };
        content.Paint += (_, e) =>
        {
            using var bg = new LinearGradientBrush(content.ClientRectangle, Color.FromArgb(12, 20, 36), Color.FromArgb(5, 8, 15), 90f);
            e.Graphics.FillRectangle(bg, content.ClientRectangle);
            using var gridPen = new Pen(Color.FromArgb(13, 70, 112, 170), 1);
            for (int x = 0; x < content.Width; x += 42) e.Graphics.DrawLine(gridPen, x, 0, x, content.Height);
            for (int y = 0; y < content.Height; y += 42) e.Graphics.DrawLine(gridPen, 0, y, content.Width, y);
        };
        Controls.Add(content);

        var hero = MakeCard(new Point(28, 20), new Size(1060, 250), Color.FromArgb(21, 32, 56), Color.FromArgb(12, 19, 36));
        hero.Paint += (_, e) => DrawHeroArtwork(e.Graphics, hero.ClientRectangle);\n        var eyebrow = new Label { Text = "YOUR NEXT GAME STARTS HERE", AutoSize = true, Location = new Point(32, 24), ForeColor = Color.FromArgb(126, 166, 255), Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.Transparent };
        var heading = new Label { Text = "NBA 2K23", AutoSize = true, Location = new Point(28, 58), ForeColor = Color.White, Font = new Font("Segoe UI", 38F, FontStyle.Bold), BackColor = Color.Transparent };
        var sub = new Label { Text = "Find the game on this PC and launch from its local installation.", AutoSize = true, Location = new Point(34, 125), ForeColor = Color.FromArgb(176, 190, 216), BackColor = Color.Transparent };
        _playButton.Text = "▶   PLAY NOW";
        StyleButton(_playButton, new Point(34, 174), new Size(250, 54), Color.FromArgb(28, 104, 255));\n        _playButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _playButton.Enabled = false;
        _playButton.Click += (_, _) => LaunchLocalGame();
        hero.Controls.AddRange(new Control[] { eyebrow, heading, sub, _playButton });
        content.Controls.Add(hero);

        var install = MakeCard(new Point(28, 290), new Size(1060, 120), Color.FromArgb(22, 29, 47), Color.FromArgb(15, 21, 36));
        install.Controls.Add(new Label { Text = "GAME INSTALLATION", AutoSize = true, Location = new Point(20, 14), ForeColor = Color.FromArgb(130, 164, 230), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), BackColor = Color.Transparent });
        _gamePath.Text = "Searching this computer for NBA2K23.exe…";
        _gamePath.Location = new Point(20, 43); _gamePath.Size = new Size(850, 25);
        _gamePath.ForeColor = Color.White; _gamePath.AutoEllipsis = true; _gamePath.BackColor = Color.Transparent;
        _scanStatus.Text = "Initial scan starting";
        _scanStatus.Location = new Point(20, 76); _scanStatus.AutoSize = true; _scanStatus.ForeColor = Color.FromArgb(156, 170, 196); _scanStatus.BackColor = Color.Transparent;
        install.Controls.Add(_gamePath); install.Controls.Add(_scanStatus);
        _scanButton.Text = "SCAN AGAIN";
        StyleButton(_scanButton, new Point(900, 68), new Size(135, 34), Color.FromArgb(43, 58, 88));
        _scanButton.Click += async (_, _) => await ScanComputerAsync();
        install.Controls.Add(_scanButton);
        content.Controls.Add(install);

        var server = MakeCard(new Point(28, 430), new Size(1060, 150), Color.FromArgb(22, 29, 47), Color.FromArgb(15, 21, 36));
        server.Controls.Add(new Label { Text = "COMMUNITY SERVICE STATUS  ·  OPTIONAL", AutoSize = true, Location = new Point(20, 15), ForeColor = Color.FromArgb(130, 164, 230), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), BackColor = Color.Transparent });
        _serverUrl.Location = new Point(20, 48); _serverUrl.Size = new Size(780, 32);
        _serverUrl.BackColor = Color.FromArgb(10, 16, 29); _serverUrl.ForeColor = Color.White; _serverUrl.BorderStyle = BorderStyle.FixedSingle;
        server.Controls.Add(_serverUrl);
        var check = new Button { Text = "CHECK", Location = new Point(690, 46), Size = new Size(100, 32) };
        StyleButton(check, check.Location, check.Size, Color.FromArgb(43, 58, 88));
        check.Click += async (_, _) => { SaveSettings(); await RefreshServerAsync(); };
        server.Controls.Add(check);
        _serverStatus.Text = "Not checked";
        _serverStatus.Location = new Point(20, 94); _serverStatus.AutoSize = true; _serverStatus.ForeColor = Color.FromArgb(170, 185, 210); _serverStatus.BackColor = Color.Transparent;
        server.Controls.Add(_serverStatus);
        content.Controls.Add(server);

        var foot = new Label
        {
            Text = "QUBE LAUNCHER  •  LOCAL FILES ONLY  •  No game files are downloaded or modified. If this installation requires Steam or another ownership check, this launcher will not bypass it. NBA 2K23 official online services have been retired; this optional check does not restore official multiplayer.",
            Location = new Point(30, 600), Size = new Size(1030, 48), ForeColor = Color.FromArgb(116, 131, 158), Font = new Font("Segoe UI", 8F), BackColor = Color.Transparent
        };
        content.Controls.Add(foot);
    }

    private static void DrawHeroArtwork(Graphics g, Rectangle bounds)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var glow = new LinearGradientBrush(bounds, Color.FromArgb(30, 62, 112), Color.FromArgb(8, 13, 27), 0f);
        g.FillRectangle(glow, bounds);
        using var beam = new Pen(Color.FromArgb(34, 112, 170, 255), 3);
        for (int i = 0; i < 7; i++)
            g.DrawLine(beam, bounds.Width - 360 + i * 42, 0, bounds.Width - 520 + i * 80, bounds.Height);
        using var court = new Pen(Color.FromArgb(75, 126, 169, 220), 2);
        g.DrawArc(court, bounds.Width - 390, 50, 300, 190, 185, 170);
        g.DrawLine(court, bounds.Width - 360, bounds.Height - 24, bounds.Width - 35, bounds.Height - 24);
        // Original stylized basketball artwork, not copied game or player imagery.
        int bx = bounds.Width - 245, by = 42, d = 150;
        using var ballBrush = new LinearGradientBrush(new Rectangle(bx, by, d, d),
            Color.FromArgb(255, 173, 65), Color.FromArgb(170, 55, 20), 45f);
        g.FillEllipse(ballBrush, bx, by, d, d);
        using var ballPen = new Pen(Color.FromArgb(230, 35, 20, 16), 4);
        g.DrawEllipse(ballPen, bx, by, d, d);
        g.DrawArc(ballPen, bx + 34, by - 4, 82, d + 8, 90, 180);
        g.DrawArc(ballPen, bx - 4, by + 34, d + 8, 82, 0, 180);
        using var shine = new Pen(Color.FromArgb(65, 255, 255, 255), 7);
        g.DrawArc(shine, bx + 12, by + 10, 55, 55, 205, 95);
        using var shadow = new SolidBrush(Color.FromArgb(65, 0, 0, 0));
        g.FillEllipse(shadow, bx - 20, by + d - 4, d + 40, 24);
    }

    private static Panel MakeCard(Point location, Size size, Color top, Color bottom)
    {
        var panel = new Panel { Location = location, Size = size, BackColor = top, Padding = new Padding(0) };
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 18);
            using var brush = new LinearGradientBrush(panel.ClientRectangle, top, bottom, 90f);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(Color.FromArgb(48, 86, 120, 180), 1);
            e.Graphics.DrawPath(pen, path);
        };
        return panel;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void StyleButton(Button button, Point location, Size size, Color color)
    {
        button.Location = location; button.Size = size;
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0;
        button.BackColor = color; button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    private async Task ScanComputerAsync()
    {
        if (_scanning) return;
        _scanning = true; _scanButton.Enabled = false; _playButton.Enabled = false;
        _scanStatus.Text = "Scanning common install folders and local drives…";
        _gamePath.Text = "Looking for NBA2K23.exe";
        try
        {
            var found = await Task.Run(FindGameExecutable);
            _gameExe = found;
            if (found is not null)
            {
                _gamePath.Text = found;
                _scanStatus.Text = "Game executable found. Ready to launch.";
                _playButton.Enabled = true;
                SaveSettings();
            }
            else
            {
                _gamePath.Text = "No NBA2K23.exe found on accessible drives.";
                _scanStatus.Text = "Use a manual folder selection only if you know where the game is installed.";
                var choose = MessageBox.Show("No game executable was found. Would you like to select the game folder manually?",
                    "Game not found", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (choose == DialogResult.Yes) ChooseGameFolder();
            }
        }
        catch (Exception ex) { _scanStatus.Text = "Scan error: " + ex.Message; }
        finally { _scanning = false; _scanButton.Enabled = true; }
    }

    private static string? FindGameExecutable()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] roots = {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Games"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Games"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };
        foreach (var root in roots.Where(Directory.Exists)) candidates.Add(root);
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable)))
            candidates.Add(drive.RootDirectory.FullName);

        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Windows", "$Recycle.Bin", "System Volume Information", "ProgramData", "WindowsApps",
            "Microsoft", "Packages", "node_modules", ".git"
        };
        foreach (var root in candidates)
        {
            var hit = SearchDirectory(root, 0, 5, skip);
            if (hit is not null) return hit;
        }
        return null;
    }

    private static string? SearchDirectory(string directory, int depth, int maxDepth, HashSet<string> skip)
    {
        try
        {
            var direct = Path.Combine(directory, "NBA2K23.exe");
            if (File.Exists(direct)) return direct;
            if (depth >= maxDepth) return null;
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var name = Path.GetFileName(child);
                if (skip.Contains(name) || name.StartsWith('.') || name.Equals("Temp", StringComparison.OrdinalIgnoreCase)) continue;
                var result = SearchDirectory(child, depth + 1, maxDepth, skip);
                if (result is not null) return result;
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        catch (System.Security.SecurityException) { }
        return null;
    }

    private void ChooseGameFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select the folder containing NBA2K23.exe", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var exe = Path.Combine(dialog.SelectedPath, "NBA2K23.exe");
        if (!File.Exists(exe))
        {
            MessageBox.Show("That folder does not contain NBA2K23.exe.", "Game not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _gameExe = exe; _gamePath.Text = exe; _scanStatus.Text = "Executable found. Ready to launch.";
        _playButton.Enabled = true; SaveSettings();
    }

    private void LaunchLocalGame()
    {
        if (string.IsNullOrWhiteSpace(_gameExe) || !File.Exists(_gameExe))
        {
            _playButton.Enabled = false;
            MessageBox.Show("The selected game executable is missing. Scan again to locate it.", "Game files missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _gameExe, WorkingDirectory = Path.GetDirectoryName(_gameExe)!, UseShellExecute = true });
            _scanStatus.Text = "Launch request sent to the local game executable.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Windows could not start the game. If the installation requires Steam or another ownership check, use its supported launch method.\n\n" + ex.Message,
                "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshServerAsync()
    {
        if (!Uri.TryCreate(_serverUrl.Text.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        { _serverStatus.Text = "Enter a valid HTTP(S) status URL."; return; }
        try
        {
            using var response = await Http.GetAsync(uri);
            _serverStatus.Text = response.IsSuccessStatusCode ? "Community endpoint: ONLINE" : $"Community endpoint: HTTP {(int)response.StatusCode}";
        }
        catch { _serverStatus.Text = "Community endpoint: OFFLINE / unreachable"; }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            if (doc.RootElement.TryGetProperty("statusEndpoint", out var endpoint)) _serverUrl.Text = endpoint.GetString() ?? "";
            if (doc.RootElement.TryGetProperty("gameExe", out var exe))
            {
                var saved = exe.GetString();
                if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
                { _gameExe = saved; _gamePath.Text = saved; _scanStatus.Text = "Saved installation found."; _playButton.Enabled = true; }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new { statusEndpoint = _serverUrl.Text.Trim(), gameExe = _gameExe }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
