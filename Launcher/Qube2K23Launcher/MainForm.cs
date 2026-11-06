using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.Json;

namespace Qube2K23Launcher;

public sealed class MainForm : Form
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly Label _gamePath = new();
    private readonly Label _installStatus = new();
    private readonly Label _serverStatus = new();
    private readonly TextBox _serverUrl = new();
    private readonly Button _playButton = new();
    private readonly Button _scanButton = new();
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QUBE", "2K23Launcher", "settings.json");
    private string? _gameExe;
    private bool _scanning;

    public MainForm()
    {
        Text = "QUBE 2K23";
        MinimumSize = new Size(1050, 720);
        Size = new Size(1440, 900);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(5, 8, 17);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;
        BuildInterface();
        LoadSettings();
        Shown += async (_, _) => await ScanComputerAsync();
    }

    private void BuildInterface()
    {
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(5, 8, 17) };
        Controls.Add(shell);

        var top = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Color.FromArgb(8, 12, 24) };
        top.Paint += (_, e) =>
        {
            using var b = new LinearGradientBrush(top.ClientRectangle, Color.FromArgb(17, 27, 51), Color.FromArgb(5, 8, 17), 0f);
            e.Graphics.FillRectangle(b, top.ClientRectangle);
            using var p = new Pen(Color.FromArgb(40, 75, 130), 1);
            e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1);
        };
        var logo = new Label { Text = "QUBE", Location = new Point(28, 8), Size = new Size(190, 42), Font = new Font("Segoe UI Black", 27F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.White, BackColor = Color.Transparent };
        var logoSub = new Label { Text = "2K23", Location = new Point(30, 47), Size = new Size(90, 25), Font = new Font("Segoe UI Black", 17F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.FromArgb(255, 45, 65), BackColor = Color.Transparent };
        top.Controls.AddRange(new Control[] { logo, logoSub });
        AddTopTab(top, "HOME", 250, true);
        AddTopTab(top, "PLAY", 390, false);
        AddTopTab(top, "UPGRADES", 505, false);
        AddTopTab(top, "SETTINGS", 675, false);
        AddTopTab(top, "MORE ⌄", 850, false);
        shell.Controls.Add(top);

        var side = new Panel { Dock = DockStyle.Left, Width = 176, BackColor = Color.FromArgb(7, 11, 22) };
        side.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(35, 75, 125), 1);
            e.Graphics.DrawLine(p, side.Width - 1, 0, side.Width - 1, side.Height);
        };
        AddSideItem(side, "⌂   Home", 82, true);
        AddSideItem(side, "⚑   Play", 142, false);
        AddSideItem(side, "▥   MyCAREER", 202, false);
        AddSideItem(side, "🛒   Store", 262, false);
        AddSideItem(side, "⚙   Settings", 322, false);
        AddSideItem(side, "?   Support", 382, false);
        shell.Controls.Add(side);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(5, 8, 16) };
        footer.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(40, 65, 105));
            e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
        };
        footer.Controls.Add(FooterLabel("◉  CPU —", 28, Color.Gainsboro));
        footer.Controls.Add(FooterLabel("▣  GPU —", 145, Color.FromArgb(40, 255, 110)));
        footer.Controls.Add(FooterLabel("▥  FPS —", 270, Color.FromArgb(40, 255, 110)));
        footer.Controls.Add(FooterLabel("NEW", 405, Color.FromArgb(255, 55, 65)));
        footer.Controls.Add(FooterLabel("Welcome to QUBE 2K23!  Your court is waiting.", 465, Color.Gainsboro));
        var slogan = FooterLabel("BUILT FOR THE COMMUNITY", 0, Color.FromArgb(160, 170, 195));
        slogan.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        slogan.Location = new Point(Math.Max(footer.Width - 250, 700), 19);
        footer.Resize += (_, _) => slogan.Location = new Point(Math.Max(footer.Width - 270, 700), 19);
        footer.Controls.Add(slogan);
        shell.Controls.Add(footer);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(7, 11, 22), Padding = new Padding(24, 18, 24, 14) };
        body.Paint += (_, e) => DrawArenaBackdrop(e.Graphics, body.ClientRectangle);
        shell.Controls.Add(body);

        var hero = new Panel { Dock = DockStyle.Top, Height = 500, BackColor = Color.FromArgb(9, 15, 31) };
        hero.Paint += (_, e) => DrawHero(e.Graphics, hero.ClientRectangle);
        body.Controls.Add(hero);

        var tagline = new Label { Text = "PLAY. COMPETE. DOMINATE.", Location = new Point(38, 238), Size = new Size(520, 42), Font = new Font("Segoe UI", 17F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.White, BackColor = Color.Transparent };
        hero.Controls.Add(tagline);
        _playButton.Text = "▶    PLAY NOW";
        _playButton.Location = new Point(42, 310);
        _playButton.Size = new Size(430, 78);
        _playButton.Font = new Font("Segoe UI Black", 20F, FontStyle.Bold | FontStyle.Italic);
        StyleButton(_playButton, Color.FromArgb(0, 75, 220));
        _playButton.Enabled = false;
        _playButton.Click += (_, _) => LaunchLocalGame();
        hero.Controls.Add(_playButton);

        var cards = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 172, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        body.Controls.Add(cards);
        cards.Controls.Add(BuildInstallCard(), 0, 0);
        cards.Controls.Add(BuildServerCard(), 1, 0);
        cards.Controls.Add(BuildRewardsCard(), 2, 0);
        body.Controls.SetChildIndex(cards, 0);
    }

    private static void DrawArenaBackdrop(Graphics g, Rectangle r)
    {
        using var b = new LinearGradientBrush(r, Color.FromArgb(13, 24, 49), Color.FromArgb(4, 7, 15), 90f);
        g.FillRectangle(b, r);
        using var p = new Pen(Color.FromArgb(15, 105, 150, 255), 1);
        for (int x = 0; x < r.Width; x += 52) g.DrawLine(p, x, 0, x, r.Height);
        using var glow = new SolidBrush(Color.FromArgb(18, 20, 100, 255));
        g.FillEllipse(glow, r.Width / 2 - 180, -190, 600, 420);
    }

    private static void DrawHero(Graphics g, Rectangle r)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(r, Color.FromArgb(8, 15, 31), Color.FromArgb(16, 31, 61), 0f);
        g.FillRectangle(bg, r);
        using var dark = new LinearGradientBrush(r, Color.FromArgb(5, 8, 17), Color.FromArgb(5, 8, 17, 0), 0f);
        g.FillRectangle(dark, r);
        using var light = new Pen(Color.FromArgb(75, 110, 165, 255), 4);
        for (int i = 0; i < 9; i++)
        {
            int x = r.Width - 470 + i * 48;
            g.DrawLine(light, x, 0, x - 170, r.Height);
        }
        using var arena = new Pen(Color.FromArgb(75, 125, 165, 230), 2);
        g.DrawArc(arena, r.Width - 650, 100, 610, 360, 190, 155);
        g.DrawLine(arena, r.Width - 680, r.Height - 20, r.Width - 20, r.Height - 20);
        using var floor = new LinearGradientBrush(new Rectangle(0, r.Height - 105, r.Width, 105), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(190, 0, 5, 15), 90f);
        g.FillRectangle(floor, 0, r.Height - 105, r.Width, 105);

        // Original, vector-drawn QUBE athletes keep the artwork crisp at every window size.
        DrawPlayer(g, r.Width - 570, 105, 0.88f, Color.FromArgb(22, 92, 205), Color.FromArgb(235, 240, 250));
        DrawPlayer(g, r.Width - 390, 68, 1.08f, Color.FromArgb(190, 35, 55), Color.FromArgb(245, 235, 225));
        DrawPlayer(g, r.Width - 205, 118, 0.82f, Color.FromArgb(35, 125, 105), Color.FromArgb(235, 240, 250));
        DrawBasketball(g, r.Width - 335, r.Height - 180, 92);

        using var eyebrowBrush = new SolidBrush(Color.FromArgb(80, 170, 245));
        using var titleBrush = new SolidBrush(Color.FromArgb(238, 245, 250));
        using var redBrush = new SolidBrush(Color.FromArgb(235, 30, 52));
        using var eyebrowFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        using var titleFont = new Font("Segoe UI Black", 66F, FontStyle.Bold | FontStyle.Italic);
        using var redFont = new Font("Segoe UI Black", 75F, FontStyle.Bold | FontStyle.Italic);
        g.DrawString("YOUR NEXT GAME STARTS HERE", eyebrowFont, eyebrowBrush, 40, 35);
        g.DrawString("QUBE", titleFont, titleBrush, 36, 75);
        g.DrawString("2K23", redFont, redBrush, 36, 135);
    }

    private static void DrawPlayer(Graphics g, int x, int y, float scale, Color jersey, Color trim)
    {
        var state = g.Save();
        g.TranslateTransform(x, y);
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var skin = new SolidBrush(Color.FromArgb(165, 105, 78));
        using var shorts = new SolidBrush(Color.FromArgb(12, 19, 34));
        using var jerseyBrush = new LinearGradientBrush(new Rectangle(12, 58, 100, 128), ControlPaint.Light(jersey), jersey, 0f);
        using var outline = new Pen(Color.FromArgb(220, 8, 12, 22), 5);
        using var accent = new Pen(trim, 5);
        // Head and neck
        g.FillEllipse(skin, 43, 8, 43, 48);
        g.FillRectangle(skin, 54, 48, 22, 20);
        // Arms
        using var arm = new Pen(skin, 20) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(arm, 25, 72, 3, 142);
        g.DrawLine(arm, 98, 72, 126, 132);
        // Jersey torso
        Point[] torso = { new(30, 58), new(86, 58), new(111, 178), new(8, 178) };
        g.FillPolygon(jerseyBrush, torso);
        g.DrawPolygon(outline, torso);
        g.DrawLine(accent, 31, 61, 85, 61);
        g.DrawLine(accent, 31, 62, 22, 166);
        g.DrawLine(accent, 85, 62, 96, 166);
        // Shorts and legs
        g.FillRectangle(shorts, 15, 174, 90, 48);
        g.DrawLine(outline, 59, 178, 59, 220);
        using var leg = new Pen(skin, 22) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(leg, 36, 218, 28, 286);
        g.DrawLine(leg, 84, 218, 96, 286);
        using var shoe = new SolidBrush(trim);
        g.FillRectangle(shoe, 9, 278, 42, 15);
        g.FillRectangle(shoe, 78, 278, 42, 15);
        g.Restore(state);
    }

    private static void DrawBasketball(Graphics g, int x, int y, int d)
    {
        using var shadow = new SolidBrush(Color.FromArgb(95, 0, 0, 0));
        g.FillEllipse(shadow, x - 18, y + d - 4, d + 36, 28);
        using var ball = new LinearGradientBrush(new Rectangle(x, y, d, d), Color.FromArgb(255, 180, 70), Color.FromArgb(150, 42, 18), 45f);
        g.FillEllipse(ball, x, y, d, d);
        using var seam = new Pen(Color.FromArgb(235, 45, 22, 15), 5);
        g.DrawEllipse(seam, x, y, d, d);
        g.DrawArc(seam, x + d / 3, y - 3, d / 3, d + 6, 90, 180);
        g.DrawArc(seam, x - 3, y + d / 3, d + 6, d / 3, 0, 180);
        using var shine = new Pen(Color.FromArgb(80, 255, 255, 255), 9);
        g.DrawArc(shine, x + 18, y + 12, 65, 65, 205, 95);
    }

    private Panel BuildInstallCard()
    {
        var p = MakeCard("GAME INSTALLATION", Color.FromArgb(10, 18, 35));
        _installStatus.Text = "●  Searching for game files…";
        _installStatus.Location = new Point(18, 42); _installStatus.Size = new Size(330, 26);
        _installStatus.ForeColor = Color.FromArgb(255, 190, 70); _installStatus.BackColor = Color.Transparent;
        _gamePath.Text = "Looking for NBA2K23.exe";
        _gamePath.Location = new Point(18, 72); _gamePath.Size = new Size(340, 28);
        _gamePath.ForeColor = Color.Gainsboro; _gamePath.AutoEllipsis = true; _gamePath.BackColor = Color.Transparent;
        p.Controls.Add(_installStatus); p.Controls.Add(_gamePath);
        _scanButton.Text = "CHANGE FOLDER / SCAN";
        _scanButton.Location = new Point(18, 112); _scanButton.Size = new Size(230, 34);
        StyleButton(_scanButton, Color.FromArgb(28, 39, 65));
        _scanButton.Click += async (_, _) => { ChooseGameFolder(); if (_gameExe is null) await ScanComputerAsync(); };
        p.Controls.Add(_scanButton);
        return p;
    }

    private Panel BuildServerCard()
    {
        var p = MakeCard("ONLINE / COMMUNITY STATUS", Color.FromArgb(9, 20, 42));
        _serverStatus.Text = "●  Not checked";
        _serverStatus.Location = new Point(18, 43); _serverStatus.Size = new Size(320, 26);
        _serverStatus.ForeColor = Color.FromArgb(170, 185, 210); _serverStatus.BackColor = Color.Transparent;
        p.Controls.Add(_serverStatus);
        _serverUrl.Location = new Point(18, 76); _serverUrl.Size = new Size(220, 28);
        _serverUrl.BackColor = Color.FromArgb(7, 12, 24); _serverUrl.ForeColor = Color.White; _serverUrl.BorderStyle = BorderStyle.FixedSingle;
        p.Controls.Add(_serverUrl);
        var check = new Button { Text = "CHECK", Location = new Point(248, 75), Size = new Size(92, 30) };
        StyleButton(check, Color.FromArgb(30, 50, 95));
        check.Click += async (_, _) => { SaveSettings(); await RefreshServerAsync(); };
        p.Controls.Add(check);
        p.Controls.Add(MutedLabel("Community endpoint only; not official multiplayer.", 18, 115, 350));
        return p;
    }

    private Panel BuildRewardsCard()
    {
        var p = MakeCard("QUBE COMMUNITY", Color.FromArgb(24, 10, 39));
        var heading = new Label { Text = "BUILT FOR THE COMMUNITY", Location = new Point(18, 48), Size = new Size(350, 30), Font = new Font("Segoe UI Black", 15F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.White, BackColor = Color.Transparent };
        var desc = MutedLabel("Play local installations. Stay connected with community updates.", 18, 82, 350);
        var button = new Button { Text = "COMMUNITY INFO  ›", Location = new Point(18, 116), Size = new Size(220, 32) };
        StyleButton(button, Color.FromArgb(60, 26, 105));
        button.Click += (_, _) => MessageBox.Show("QUBE community features can be connected here when an official community endpoint is configured.", "QUBE Community", MessageBoxButtons.OK, MessageBoxIcon.Information);
        p.Controls.AddRange(new Control[] { heading, desc, button });
        return p;
    }

    private static Panel MakeCard(string title, Color color)
    {
        var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5), BackColor = color };
        p.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 14);
            using var b = new LinearGradientBrush(p.ClientRectangle, color, Color.FromArgb(7, 11, 23), 90f);
            e.Graphics.FillPath(b, path);
            using var pen = new Pen(Color.FromArgb(100, 75, 115, 190), 1);
            e.Graphics.DrawPath(pen, path);
        };
        p.Controls.Add(new Label { Text = title, Location = new Point(18, 14), Size = new Size(360, 22), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(205, 220, 245), BackColor = Color.Transparent });
        return p;
    }

    private static GraphicsPath RoundedRect(Rectangle b, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(b.X, b.Y, d, d, 180, 90); p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
        p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90); p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }

    private static void StyleButton(Button b, Color color)
    {
        b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = Color.FromArgb(80, 100, 175, 255);
        b.BackColor = color; b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI", 9F, FontStyle.Bold); b.Cursor = Cursors.Hand;
    }

    private static Label MutedLabel(string text, int x, int y, int w) =>
        new() { Text = text, Location = new Point(x, y), Size = new Size(w, 24), ForeColor = Color.FromArgb(155, 170, 195), Font = new Font("Segoe UI", 8F), BackColor = Color.Transparent };

    private static Label FooterLabel(string text, int x, Color color) =>
        new() { Text = text, Location = new Point(x, 18), AutoSize = true, ForeColor = color, Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.Transparent };

    private static void AddTopTab(Panel p, string text, int x, bool selected)
    {
        var l = new Label { Text = text, Location = new Point(x, 0), Size = new Size(125, 80), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = selected ? Color.White : Color.FromArgb(175, 185, 205), BackColor = selected ? Color.FromArgb(25, 45, 85) : Color.Transparent, Cursor = Cursors.Hand };
        l.Click += (_, _) => { if (text == "PLAY") MessageBox.Show("Select PLAY NOW after a supported local game installation is detected.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information); };
        p.Controls.Add(l);
    }

    private static void AddSideItem(Panel p, string text, int y, bool selected)
    {
        var l = new Label { Text = text, Location = new Point(0, y), Size = new Size(175, 48), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = selected ? Color.White : Color.FromArgb(170, 180, 200), BackColor = selected ? Color.FromArgb(20, 45, 95) : Color.Transparent, Cursor = Cursors.Hand };
        p.Controls.Add(l);
    }

    private async Task ScanComputerAsync()
    {
        if (_scanning) return;
        _scanning = true; _scanButton.Enabled = false; _playButton.Enabled = false;
        _installStatus.Text = "●  Scanning local drives…";
        _installStatus.ForeColor = Color.FromArgb(255, 190, 70);
        try
        {
            var found = await Task.Run(FindGameExecutable);
            _gameExe = found;
            if (found is not null)
            {
                _gamePath.Text = found; _installStatus.Text = "●  GAME FOUND";
                _installStatus.ForeColor = Color.Lime; _playButton.Enabled = true; SaveSettings();
            }
            else
            {
                _gamePath.Text = "No NBA2K23.exe found on accessible drives.";
                _installStatus.Text = "●  GAME NOT FOUND"; _installStatus.ForeColor = Color.OrangeRed;
            }
        }
        catch (Exception ex) { _installStatus.Text = "●  Scan error"; _gamePath.Text = ex.Message; }
        finally { _scanning = false; _scanButton.Enabled = true; }
    }

    private static string? FindGameExecutable()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Games"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") })
            if (Directory.Exists(p)) roots.Add(p);
        foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))) roots.Add(d.RootDirectory.FullName);
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Windows", "$Recycle.Bin", "System Volume Information", "WindowsApps", "ProgramData", "node_modules", ".git" };
        foreach (var root in roots) { var hit = SearchDirectory(root, 0, 5, skip); if (hit is not null) return hit; }
        return null;
    }

    private static string? SearchDirectory(string dir, int depth, int maxDepth, HashSet<string> skip)
    {
        try
        {
            var exe = Path.Combine(dir, "NBA2K23.exe");
            if (File.Exists(exe)) return exe;
            if (depth >= maxDepth) return null;
            foreach (var child in Directory.EnumerateDirectories(dir))
            {
                var name = Path.GetFileName(child);
                if (skip.Contains(name) || name.StartsWith('.') || name.Equals("Temp", StringComparison.OrdinalIgnoreCase)) continue;
                var found = SearchDirectory(child, depth + 1, maxDepth, skip);
                if (found is not null) return found;
            }
        }
        catch (UnauthorizedAccessException) { } catch (IOException) { } catch (System.Security.SecurityException) { }
        return null;
    }

    private void ChooseGameFolder()
    {
        using var d = new FolderBrowserDialog { Description = "Select the folder containing NBA2K23.exe", UseDescriptionForTitle = true };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var exe = Path.Combine(d.SelectedPath, "NBA2K23.exe");
        if (!File.Exists(exe)) { MessageBox.Show("That folder does not contain NBA2K23.exe.", "Game not found", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        _gameExe = exe; _gamePath.Text = exe; _installStatus.Text = "●  GAME FOUND"; _installStatus.ForeColor = Color.Lime; _playButton.Enabled = true; SaveSettings();
    }

    private void LaunchLocalGame()
    {
        if (string.IsNullOrWhiteSpace(_gameExe) || !File.Exists(_gameExe))
        {
            _playButton.Enabled = false; MessageBox.Show("The selected executable is missing. Scan again to locate the game.", "Game files missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _gameExe, WorkingDirectory = Path.GetDirectoryName(_gameExe)!, UseShellExecute = true });
            _installStatus.Text = "●  LAUNCH REQUEST SENT";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Windows could not start this executable. If the installation requires Steam or another ownership check, this launcher will not bypass it.\n\n" + ex.Message, "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshServerAsync()
    {
        if (!Uri.TryCreate(_serverUrl.Text.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        { _serverStatus.Text = "●  Enter a valid HTTP(S) status URL."; return; }
        try
        {
            using var response = await Http.GetAsync(uri);
            _serverStatus.Text = response.IsSuccessStatusCode ? "●  COMMUNITY ENDPOINT ONLINE" : $"●  HTTP {(int)response.StatusCode}";
            _serverStatus.ForeColor = response.IsSuccessStatusCode ? Color.Lime : Color.OrangeRed;
        }
        catch { _serverStatus.Text = "●  COMMUNITY ENDPOINT OFFLINE"; _serverStatus.ForeColor = Color.OrangeRed; }
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
                { _gameExe = saved; _gamePath.Text = saved; _installStatus.Text = "●  GAME FOUND"; _installStatus.ForeColor = Color.Lime; _playButton.Enabled = true; }
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