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
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(14, 8, 27) };
        Controls.Add(shell);

        var top = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(47, 17, 91) };
        top.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(top.ClientRectangle, Color.FromArgb(91, 35, 170), Color.FromArgb(35, 13, 70), 0f);
            e.Graphics.FillRectangle(brush, top.ClientRectangle);
            using var line = new Pen(Color.FromArgb(105, 160, 100, 230));
            e.Graphics.DrawLine(line, 0, top.Height - 1, top.Width, top.Height - 1);
        };
        var mark = new Label { Text = "Q", Location = new Point(22, 13), Size = new Size(42, 44), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Black", 23F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(133, 54, 255) };
        var wordmark = new Label { Text = "QUBE 2K23", Location = new Point(76, 16), Size = new Size(172, 38), Font = new Font("Segoe UI Black", 18F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.White, BackColor = Color.Transparent };
        top.Controls.Add(mark);
        top.Controls.Add(wordmark);
        AddTopTab(top, "PLAY", 280);
        AddTopTab(top, "UPGRADES", 365);
        AddTopTab(top, "MYCAREER", 485);
        AddTopTab(top, "STORE", 625);
        AddTopTab(top, "COMMUNITY", 715);
        AddTopTab(top, "SETTINGS", 860);
        AddTopTab(top, "SUPPORT", 985);
        var online = new Label { Text = "●  DESKTOP LAUNCHER", Location = new Point(1090, 0), Size = new Size(190, 72), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(205, 255, 225), BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        top.Controls.Add(online);
        shell.Controls.Add(top);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.FromArgb(10, 5, 20) };
        footer.Controls.Add(FooterLabel("QUBE 2K23  •  BUILT FOR THE COMMUNITY", 22, Color.FromArgb(165, 150, 190)));
        shell.Controls.Add(footer);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(17, 9, 31),
            Padding = new Padding(26, 20, 26, 18), ColumnCount = 1, RowCount = 2
        };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 76F));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));
        shell.Controls.Add(body);

        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(11, 13, 31), Margin = new Padding(0, 0, 0, 12) };
        hero.Paint += (_, e) => DrawHero(e.Graphics, hero.ClientRectangle);
        body.Controls.Add(hero, 0, 0);

        var left = new Panel { Location = new Point(34, 30), Size = new Size(560, 390), BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        hero.Controls.Add(left);

        var eyebrow = new Label { Text = "WELCOME TO THE NEXT GENERATION", Location = new Point(2, 26), Size = new Size(490, 26), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(203, 166, 255), BackColor = Color.Transparent };
        var title = new Label { Text = "QUBE 2K23", Location = new Point(0, 70), Size = new Size(540, 76), Font = new Font("Segoe UI Black", 47F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.White, BackColor = Color.Transparent };
        var subtitle = new Label { Text = "PLAY. COMPETE. DOMINATE.", Location = new Point(3, 157), Size = new Size(520, 38), Font = new Font("Segoe UI", 19F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.FromArgb(242, 232, 255), BackColor = Color.Transparent };
        var description = new Label { Text = "Your court is waiting. Select a supported local game installation to get started.", Location = new Point(4, 211), Size = new Size(510, 52), Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(220, 211, 237), BackColor = Color.Transparent };
        left.Controls.AddRange(new Control[] { eyebrow, title, subtitle, description });

        _playButton.Text = "▶   PLAY NOW";
        _playButton.Location = new Point(2, 292);
        _playButton.Size = new Size(245, 58);
        _playButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        StyleButton(_playButton, Color.FromArgb(126, 39, 238));
        _playButton.Enabled = false;
        _playButton.Click += (_, _) => LaunchLocalGame();
        left.Controls.Add(_playButton);

        var choose = new Button { Text = "GAME FOLDER", Location = new Point(262, 292), Size = new Size(190, 58), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        StyleButton(choose, Color.FromArgb(47, 30, 68));
        choose.Click += (_, _) => ChooseGameFolder();
        left.Controls.Add(choose);

        var status = MakeCard("GAME STATUS", Color.FromArgb(35, 20, 59));
        status.Location = new Point(Math.Max(610, hero.ClientSize.Width - 405), 28);
        status.Size = new Size(365, Math.Min(350, Math.Max(300, hero.ClientSize.Height - 56)));
        status.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        hero.Controls.Add(status);
        hero.Resize += (_, _) =>
        {
            status.Location = new Point(Math.Max(610, hero.ClientSize.Width - 405), 28);
            status.Height = Math.Min(350, Math.Max(300, hero.ClientSize.Height - 56));
            left.Height = Math.Max(330, hero.ClientSize.Height - 56);
        };

        status.Controls.Add(MutedLabel("Your local installation and community connection.", 20, 44, 320));
        var gameTitle = new Label { Text = "GAME INSTALLATION", Location = new Point(20, 100), Size = new Size(300, 22), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent };
        _installStatus.Location = new Point(20, 130); _installStatus.Size = new Size(320, 25); _installStatus.ForeColor = Color.FromArgb(255, 190, 90); _installStatus.BackColor = Color.Transparent;
        _gamePath.Location = new Point(20, 160); _gamePath.Size = new Size(320, 42); _gamePath.AutoEllipsis = true; _gamePath.ForeColor = Color.FromArgb(220, 210, 235); _gamePath.BackColor = Color.Transparent;
        var serverTitle = new Label { Text = "COMMUNITY STATUS", Location = new Point(20, 220), Size = new Size(300, 22), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent };
        _serverStatus.Location = new Point(20, 250); _serverStatus.Size = new Size(320, 28); _serverStatus.ForeColor = Color.FromArgb(180, 205, 190); _serverStatus.BackColor = Color.Transparent;
        var check = new Button { Text = "CHECK STATUS", Location = new Point(20, 292), Size = new Size(150, 38) };
        StyleButton(check, Color.FromArgb(88, 45, 135));
        check.Click += async (_, _) => { SaveSettings(); await RefreshServerAsync(); };
        status.Controls.AddRange(new Control[] { gameTitle, _installStatus, _gamePath, serverTitle, _serverStatus, check });

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
        for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        body.Controls.Add(cards, 0, 1);
        var installStrip = MakeCard("GAME FOLDER", Color.FromArgb(34, 22, 49));
        installStrip.Margin = new Padding(0, 0, 10, 0);
        installStrip.Controls.Add(MutedLabel("Select or scan your local installation", 16, 43, 330));
        var playerStrip = MakeCard("PLAYER PROFILE", Color.FromArgb(34, 22, 49));
        playerStrip.Margin = new Padding(5, 0, 5, 0);
        playerStrip.Controls.Add(MutedLabel("Your local launcher profile", 16, 43, 330));
        var communityStrip = MakeCard("COMMUNITY", Color.FromArgb(34, 22, 49));
        communityStrip.Margin = new Padding(10, 0, 0, 0);
        communityStrip.Controls.Add(MutedLabel("Optional status endpoint", 16, 43, 330));
        cards.Controls.Add(installStrip, 0, 0);
        cards.Controls.Add(playerStrip, 1, 0);
        cards.Controls.Add(communityStrip, 2, 0);
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
        using var bg = new LinearGradientBrush(r, Color.FromArgb(9, 12, 30), Color.FromArgb(29, 12, 48), 15F);
        g.FillRectangle(bg, r);

        // Subtle arena lighting and court geometry; all branding remains live UI text.
        using var haze = new SolidBrush(Color.FromArgb(28, 112, 56, 205));
        g.FillEllipse(haze, r.Width - 760, -260, 950, 680);
        using var beams = new Pen(Color.FromArgb(22, 175, 125, 255), 2);
        for (int i = 0; i < 7; i++)
            g.DrawLine(beams, r.Width - 650 + i * 100, 0, r.Width - 900 + i * 145, r.Height);
        using var crowd = new LinearGradientBrush(new Rectangle(0, Math.Max(0, r.Height - 190), r.Width, 190), Color.FromArgb(0, 8, 9, 24), Color.FromArgb(150, 4, 5, 15), 90F);
        g.FillRectangle(crowd, 0, Math.Max(0, r.Height - 190), r.Width, 190);
        using var court = new Pen(Color.FromArgb(48, 174, 130, 235), 2);
        g.DrawArc(court, r.Width - 700, r.Height - 330, 620, 430, 190, 160);
        g.DrawLine(court, r.Width - 720, r.Height - 24, r.Width - 35, r.Height - 24);
        using var edge = new Pen(Color.FromArgb(90, 151, 75, 222), 1);
        g.DrawLine(edge, 0, r.Height - 1, r.Width, r.Height - 1);
    }

    private static void DrawPlayer(Graphics g, int x, int y, float scale, Color jersey, Color trim)
    {
        var state = g.Save();
        g.TranslateTransform(x, y);
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var skin = new LinearGradientBrush(new Rectangle(22, 10, 100, 290), Color.FromArgb(220, 160, 125), Color.FromArgb(92, 52, 42), 25f);
        using var jerseyBrush = new LinearGradientBrush(new Rectangle(10, 64, 120, 145), ControlPaint.Light(jersey, .35f), ControlPaint.Dark(jersey, .35f), 0f);
        using var shortsBrush = new LinearGradientBrush(new Rectangle(18, 184, 100, 65), Color.FromArgb(35, 46, 70), Color.FromArgb(7, 12, 24), 90f);
        using var outline = new Pen(Color.FromArgb(235, 4, 8, 18), 7) { LineJoin = LineJoin.Round };
        using var edge = new Pen(Color.FromArgb(180, trim), 3) { LineJoin = LineJoin.Round };
        using var arm = new Pen(skin, 25) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var leg = new Pen(skin, 25) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var shoe = new LinearGradientBrush(new Rectangle(0, 0, 54, 22), trim, Color.FromArgb(12, 18, 32), 0f);

        // Athletic pose with curved shoulders, tapered torso and dimensional limbs.
        g.DrawLine(arm, 35, 88, 8, 155);
        g.DrawLine(arm, 103, 83, 132, 145);
        g.FillEllipse(skin, 22, 8, 68, 76);
        using (var hair = new SolidBrush(Color.FromArgb(210, 15, 12, 14)))
            g.FillEllipse(hair, 20, 3, 72, 34);
        g.FillRectangle(skin, 46, 67, 28, 22);

        using var torso = new GraphicsPath();
        torso.AddBezier(28, 76, 12, 67, 10, 89, 17, 117);
        torso.AddLine(17, 117, 27, 194);
        torso.AddBezier(27, 194, 53, 207, 88, 207, 112, 193);
        torso.AddLine(112, 193, 124, 112);
        torso.AddBezier(124, 112, 137, 83, 116, 67, 99, 76);
        torso.AddBezier(99, 76, 82, 91, 48, 91, 28, 76);
        g.FillPath(jerseyBrush, torso);
        g.DrawPath(outline, torso);
        g.DrawPath(edge, torso);

        // Neckline, side panels and jersey number.
        using var neck = new Pen(Color.FromArgb(230, 8, 13, 25), 7);
        g.DrawArc(neck, 43, 67, 43, 32, 5, 170);
        using var panel = new Pen(Color.FromArgb(170, trim), 5);
        g.DrawLine(panel, 30, 105, 37, 183);
        g.DrawLine(panel, 104, 105, 96, 183);
        using var numberFont = new Font("Segoe UI Black", 34F, FontStyle.Bold | FontStyle.Italic);
        using var numberBrush = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
        g.DrawString("23", numberFont, numberBrush, 48, 122);

        g.FillPath(shortsBrush, RoundedRect(new Rectangle(22, 187, 96, 64), 12));
        g.DrawLine(outline, 69, 194, 69, 246);
        g.DrawLine(leg, 47, 239, 39, 300);
        g.DrawLine(leg, 94, 239, 105, 300);
        g.FillRectangle(shoe, 14, 293, 53, 20);
        g.FillRectangle(shoe, 83, 293, 55, 20);
        using var sole = new Pen(Color.FromArgb(230, 245, 248, 255), 3);
        g.DrawLine(sole, 14, 312, 67, 312);
        g.DrawLine(sole, 83, 312, 138, 312);

        // Rim light separates each player from the dark arena.
        using var rim = new Pen(Color.FromArgb(80, trim), 5);
        g.DrawArc(rim, 16, 8, 78, 78, 205, 110);
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
        button.Click += (_, _) => ShowSectionPage("QUBE COMMUNITY", "Community updates and connection status.", "COMMUNITY STATUS", "Community updates can be added when an official QUBE endpoint is configured. The status checker only verifies the URL you provide; it does not provide official NBA 2K23 multiplayer.", "OPEN SETTINGS", ShowSettingsDialog);
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

    private void AddTopTab(Panel p, string text, int x)
    {
        var l = new Label { Text = text, Location = new Point(x, 0), Size = new Size(125, 80), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(190, 205, 225), BackColor = Color.Transparent, Cursor = Cursors.Hand };
        l.Click += (_, _) => Navigate(text.Replace(" ⌄", ""));
        l.MouseEnter += (_, _) => l.ForeColor = Color.White;
        l.MouseLeave += (_, _) => l.ForeColor = Color.FromArgb(190, 205, 225);
        p.Controls.Add(l);
    }

    private void AddSideItem(Panel p, string text, int y)
    {
        var l = new Label { Text = text, Location = new Point(0, y), Size = new Size(175, 48), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(170, 185, 205), BackColor = Color.Transparent, Cursor = Cursors.Hand };
        l.Click += (_, _) => Navigate(text.Substring(text.IndexOf(' ') + 1).Trim());
        l.MouseEnter += (_, _) => { l.BackColor = Color.FromArgb(22, 42, 78); l.ForeColor = Color.White; };
        l.MouseLeave += (_, _) => { l.BackColor = Color.Transparent; l.ForeColor = Color.FromArgb(170, 185, 205); };
        p.Controls.Add(l);
    }

    private void Navigate(string destination)
    {
        switch (destination.ToUpperInvariant())
        {
            case "HOME":
                _installStatus.Text = _gameExe is not null && File.Exists(_gameExe) ? "●  GAME FOUND" : "●  SELECT OR SCAN YOUR GAME";
                break;
            case "PLAY":
                ShowSectionPage("PLAY", "Launch your supported local installation.", "GAME STATUS", _gameExe is not null && File.Exists(_gameExe) ? "Game executable selected and ready to launch." : "No game executable selected. Choose a folder from Game Installation or Settings.", "LAUNCH GAME", () =>
                {
                    if (_gameExe is not null && File.Exists(_gameExe)) LaunchLocalGame();
                    else MessageBox.Show(this, "Choose your game folder or run a scan before launching.", "Game not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                break;
            case "SETTINGS":
                ShowSettingsDialog();
                break;
            case "UPGRADES":
                ShowSectionPage("UPGRADES", "Player progression and customization.", "SERVICE STATUS", "The upgrades service is not connected yet. Player upgrades, account changes, and purchases are currently unavailable.", "VIEW SUPPORT", ShowSupportDialog);
                break;
            case "MYCAREER":
                ShowSectionPage("MYCAREER", "Your basketball journey starts here.", "MODE STATUS", "MyCAREER is not playable in this launcher yet. A supported game mode and connected career backend are required.", "VIEW SUPPORT", ShowSupportDialog);
                break;
            case "STORE":
                ShowSectionPage("QUBE STORE", "Official QUBE content and offers.", "STORE STATUS", "The store is not connected to a verified catalog or payment service. No items are for sale and no purchases can be made here.", "VIEW SUPPORT", ShowSupportDialog);
                break;
            case "SUPPORT":
                ShowSectionPage("SUPPORT", "Troubleshoot your launcher installation.", "SUPPORT TOOLS", "Open diagnostics to copy your selected game path, community endpoint, and launch-log location.", "OPEN DIAGNOSTICS", ShowSupportDialog);
                break;
            case "MORE":
                var menu = new ContextMenuStrip();
                menu.Items.Add("Open log folder", null, (_, _) => OpenLogsFolder());
                menu.Items.Add("Support & diagnostics", null, (_, _) => ShowSupportDialog());
                menu.Items.Add("About QUBE 2K23", null, (_, _) => MessageBox.Show("QUBE 2K23 Launcher\nCommunity launcher for supported local game installations.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information));
                menu.Show(Cursor.Position);
                break;
        }
    }

    private void ShowSectionPage(string title, string subtitle, string sectionTitle, string details, string actionText, Action action)
    {
        using var page = new Form
        {
            Text = $"QUBE 2K23 | {title}",
            Size = new Size(900, 620),
            MinimumSize = new Size(760, 520),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Color.FromArgb(6, 10, 21),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F)
        };
        var header = new Panel { Dock = DockStyle.Top, Height = 132, BackColor = Color.FromArgb(10, 19, 39), Padding = new Padding(34, 22, 34, 12) };
        var brand = new Label { Text = "QUBE 2K23  /  " + title, Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI Black", 19F, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.White };
        var sub = new Label { Text = subtitle, Dock = DockStyle.Top, Height = 32, Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(170, 195, 230) };
        header.Controls.Add(sub);
        header.Controls.Add(brand);
        page.Controls.Add(header);
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(42), BackColor = Color.FromArgb(7, 12, 25) };
        var section = new Label { Text = sectionTitle, Location = new Point(42, 48), Size = new Size(700, 34), Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(110, 175, 255) };
        var message = new Label { Text = details, Location = new Point(42, 94), Size = new Size(740, 112), Font = new Font("Segoe UI", 12F), ForeColor = Color.Gainsboro };
        var actionButton = new Button { Text = actionText, Location = new Point(42, 238), Size = new Size(230, 48) };
        StyleButton(actionButton, Color.FromArgb(165, 28, 48));
        actionButton.Click += (_, _) => action();
        var backButton = new Button { Text = "BACK TO HOME", Location = new Point(290, 238), Size = new Size(180, 48) };
        StyleButton(backButton, Color.FromArgb(28, 43, 70));
        backButton.Click += (_, _) => page.Close();
        content.Controls.AddRange(new Control[] { section, message, actionButton, backButton });
        page.Controls.Add(content);
        page.ShowDialog(this);
    }

    private void ShowFeatureNotice(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowSettingsDialog()
    {
        using var dialog = new Form
        {
            Text = "QUBE 2K23 Settings",
            Size = new Size(590, 310),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(8, 13, 27),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F)
        };
        var endpointLabel = new Label { Text = "Community status URL (optional)", Location = new Point(22, 22), Size = new Size(500, 24), ForeColor = Color.White };
        var endpoint = new TextBox { Text = _serverUrl.Text, Location = new Point(22, 52), Size = new Size(520, 30), BackColor = Color.FromArgb(17, 25, 43), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var gameLabel = new Label { Text = "Selected game executable", Location = new Point(22, 105), Size = new Size(500, 22), ForeColor = Color.FromArgb(175, 190, 215) };
        var gamePath = new Label { Text = _gameExe ?? "No game selected", Location = new Point(22, 130), Size = new Size(520, 38), AutoEllipsis = true, ForeColor = Color.White };
        var browse = new Button { Text = "CHOOSE GAME FOLDER", Location = new Point(22, 180), Size = new Size(190, 34) };
        StyleButton(browse, Color.FromArgb(28, 48, 85));
        browse.Click += (_, _) =>
        {
            using var folder = new FolderBrowserDialog { Description = "Select the folder containing NBA2K23.exe", UseDescriptionForTitle = true };
            if (folder.ShowDialog(dialog) != DialogResult.OK) return;
            var exe = Path.Combine(folder.SelectedPath, "NBA2K23.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show(dialog, "That folder does not contain NBA2K23.exe.", "Game not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _gameExe = exe;
            gamePath.Text = exe;
            _gamePath.Text = exe;
            _installStatus.Text = "●  GAME FOUND";
            _installStatus.ForeColor = Color.Lime;
            _playButton.Enabled = true;
            SaveSettings();
        };
        var save = new Button { Text = "SAVE SETTINGS", Location = new Point(352, 220), Size = new Size(190, 36), DialogResult = DialogResult.OK };
        StyleButton(save, Color.FromArgb(0, 75, 190));
        save.Click += (_, _) => { _serverUrl.Text = endpoint.Text.Trim(); SaveSettings(); };
        var logs = new Button { Text = "OPEN LOGS", Location = new Point(22, 220), Size = new Size(130, 36) };
        StyleButton(logs, Color.FromArgb(28, 39, 65));
        logs.Click += (_, _) => OpenLogsFolder();
        dialog.Controls.AddRange(new Control[] { endpointLabel, endpoint, gameLabel, gamePath, browse, save, logs });
        dialog.AcceptButton = save;
        dialog.ShowDialog(this);
    }

    private void ShowSupportDialog()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QUBE", "2K23Launcher", "Logs", "last-launch.txt");
        var details = $"Game: {_gameExe ?? "Not selected"}{Environment.NewLine}Status URL: {(_serverUrl.Text.Length == 0 ? "Not configured" : _serverUrl.Text)}{Environment.NewLine}Launch log: {logPath}";
        using var dialog = new Form
        {
            Text = "QUBE Support & Diagnostics",
            Size = new Size(700, 300),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(8, 13, 27),
            ForeColor = Color.White
        };
        var info = new Label { Text = "Use these details when troubleshooting launcher or game startup issues.", Location = new Point(20, 18), Size = new Size(640, 28), ForeColor = Color.White };
        var box = new TextBox { Text = details, Location = new Point(20, 55), Size = new Size(640, 105), Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(17, 25, 43), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.FixedSingle };
        var copy = new Button { Text = "COPY DIAGNOSTICS", Location = new Point(20, 190), Size = new Size(175, 36) };
        StyleButton(copy, Color.FromArgb(28, 48, 85));
        copy.Click += (_, _) => { Clipboard.SetText(details); MessageBox.Show("Diagnostic details copied.", "QUBE Support", MessageBoxButtons.OK, MessageBoxIcon.Information); };
        var logs = new Button { Text = "OPEN LOG FOLDER", Location = new Point(210, 190), Size = new Size(175, 36) };
        StyleButton(logs, Color.FromArgb(28, 48, 85));
        logs.Click += (_, _) => OpenLogsFolder();
        dialog.Controls.AddRange(new Control[] { info, box, copy, logs });
        dialog.ShowDialog(this);
    }

    private void OpenLogsFolder()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QUBE", "2K23Launcher", "Logs");
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
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

    private async void LaunchLocalGame()
    {
        if (string.IsNullOrWhiteSpace(_gameExe) || !File.Exists(_gameExe))
        {
            _playButton.Enabled = false;
            MessageBox.Show("The selected executable is missing. Scan again to locate the game.", "Game files missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _playButton.Enabled = false;
        _installStatus.Text = "●  STARTING GAME…";
        _installStatus.ForeColor = Color.FromArgb(255, 190, 70);
        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QUBE", "2K23Launcher", "Logs");
        var logPath = Path.Combine(logDirectory, "last-launch.txt");

        try
        {
            Directory.CreateDirectory(logDirectory);
            var startInfo = new ProcessStartInfo
            {
                FileName = _gameExe,
                WorkingDirectory = Path.GetDirectoryName(_gameExe)!,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            if (!process.Start())
                throw new InvalidOperationException("Windows did not create a game process.");

            await File.WriteAllTextAsync(logPath,
                $"Launch time: {DateTimeOffset.Now:O}{Environment.NewLine}" +
                $"Executable: {_gameExe}{Environment.NewLine}" +
                $"Working directory: {startInfo.WorkingDirectory}{Environment.NewLine}" +
                $"Process ID: {process.Id}{Environment.NewLine}" +
                $"Initial status: process started{Environment.NewLine}");

            // Give the game time to initialize before deciding whether it exited immediately.
            await Task.Delay(TimeSpan.FromSeconds(12));
            if (process.HasExited)
            {
                var exitCode = process.ExitCode;
                await File.AppendAllTextAsync(logPath, $"Exited after startup; exit code: {exitCode}{Environment.NewLine}");
                _installStatus.Text = $"●  GAME EXITED ({exitCode})";
                _installStatus.ForeColor = Color.OrangeRed;
                MessageBox.Show(
                    $"The game process started, then exited after startup. Exit code: {exitCode}.\n\n" +
                    $"A diagnostic log was saved here:\n{logPath}\n\n" +
                    "This can happen because of missing/corrupt game files, missing runtime components, graphics-driver issues, or an ownership/DRM check. This launcher does not bypass ownership checks.",
                    "Game closed during startup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                // The game remained alive through its startup window. Close QUBE only
                // after this check; the game process continues independently.
                await File.AppendAllTextAsync(logPath, $"Startup confirmed at {DateTimeOffset.Now:O}; closing launcher.{Environment.NewLine}");
                process.Dispose();
                Close();
                return;
            }
        }
        catch (Exception ex)
        {
            _installStatus.Text = "●  LAUNCH FAILED";
            _installStatus.ForeColor = Color.OrangeRed;
            try
            {
                Directory.CreateDirectory(logDirectory);
                await File.WriteAllTextAsync(logPath, $"Launch failed: {DateTimeOffset.Now:O}{Environment.NewLine}{ex}");
            }
            catch { }
            MessageBox.Show("Windows could not start the game.\n\n" + ex.Message, "Launch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed && !Disposing)
                _playButton.Enabled = _gameExe is not null && File.Exists(_gameExe);
        }
    }

    private async Task MonitorGameExitAsync(Process process, string logPath)
    {
        try
        {
            await Task.Run(() => process.WaitForExit());
            var exitCode = process.ExitCode;
            await File.AppendAllTextAsync(logPath, $"Game process exited at {DateTimeOffset.Now:O}; exit code: {exitCode}{Environment.NewLine}");
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke(() =>
                {
                    _installStatus.Text = $"●  GAME CLOSED ({exitCode})";
                    _installStatus.ForeColor = Color.FromArgb(255, 190, 70);
                });
        }
        catch (Exception ex)
        {
            try { await File.AppendAllTextAsync(logPath, $"Monitor error: {ex.Message}{Environment.NewLine}"); } catch { }
        }
        finally { process.Dispose(); }
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