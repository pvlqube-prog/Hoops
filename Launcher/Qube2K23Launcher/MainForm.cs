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
        AddTopTab(top, "HOME", 250);
        AddTopTab(top, "PLAY", 390);
        AddTopTab(top, "UPGRADES", 505);
        AddTopTab(top, "SETTINGS", 675);
        AddTopTab(top, "MORE ⌄", 850);
        shell.Controls.Add(top);

        var side = new Panel { Dock = DockStyle.Left, Width = 176, BackColor = Color.FromArgb(7, 11, 22) };
        side.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(35, 75, 125), 1);
            e.Graphics.DrawLine(p, side.Width - 1, 0, side.Width - 1, side.Height);
        };
        AddSideItem(side, "⌂   Home", 82);
        AddSideItem(side, "⚑   Play", 142);
        AddSideItem(side, "▥   MyCAREER", 202);
        AddSideItem(side, "🛒   Store", 262);
        AddSideItem(side, "⚙   Settings", 322);
        AddSideItem(side, "?   Support", 382);
        shell.Controls.Add(side);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(5, 8, 16) };
        footer.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(40, 65, 105));
            e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
        };
        footer.Controls.Add(FooterLabel("●  QUBE 2K23", 28, Color.FromArgb(105, 180, 255)));
        footer.Controls.Add(FooterLabel("GAME LAUNCHER", 190, Color.FromArgb(155, 170, 195)));
        footer.Controls.Add(FooterLabel("Welcome to QUBE 2K23. Your court is waiting.", 365, Color.Gainsboro));
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
        using var bg = new LinearGradientBrush(r, Color.FromArgb(5, 10, 23), Color.FromArgb(17, 32, 62), 18f);
        g.FillRectangle(bg, r);

        // Arena atmosphere: overhead lights, haze, crowd band and a reflective court.
        using (var haze = new SolidBrush(Color.FromArgb(34, 30, 105, 220)))
            g.FillEllipse(haze, r.Width - 760, -250, 900, 650);
        using (var glow = new SolidBrush(Color.FromArgb(18, 80, 135, 255)))
            g.FillEllipse(glow, r.Width - 690, -140, 760, 520);
        using var crowd = new LinearGradientBrush(new Rectangle(0, 260, r.Width, 115), Color.FromArgb(0, 15, 25, 45), Color.FromArgb(130, 2, 5, 13), 90f);
        g.FillRectangle(crowd, 0, 260, r.Width, 115);
        using var beams = new Pen(Color.FromArgb(24, 150, 190, 255), 3);
        for (int i = 0; i < 8; i++)
            g.DrawLine(beams, r.Width - 650 + i * 85, 0, r.Width - 820 + i * 135, 360);

        // Court markings and bright center-court reflection.
        using var court = new Pen(Color.FromArgb(65, 110, 155, 225), 2);
        g.DrawArc(court, r.Width - 710, 155, 650, 410, 195, 150);
        g.DrawLine(court, r.Width - 680, r.Height - 25, r.Width - 30, r.Height - 25);
        using var courtGlow = new LinearGradientBrush(new Rectangle(0, r.Height - 125, r.Width, 125), Color.FromArgb(0, 15, 35, 70), Color.FromArgb(190, 2, 5, 13), 90f);
        g.FillRectangle(courtGlow, 0, r.Height - 125, r.Width, 125);

        // Original QUBE athletes, lit like a cinematic game-cover scene.
        DrawPlayer(g, r.Width - 585, 104, 1.02f, Color.FromArgb(22, 92, 205), Color.FromArgb(125, 190, 255));
        DrawPlayer(g, r.Width - 390, 53, 1.22f, Color.FromArgb(190, 30, 52), Color.FromArgb(255, 185, 105));
        DrawPlayer(g, r.Width - 175, 115, 0.94f, Color.FromArgb(25, 125, 105), Color.FromArgb(120, 255, 210));
        DrawBasketball(g, r.Width - 355, r.Height - 170, 82);

        // Left-side title treatment with a red accent rail and layered typography.
        using var rail = new LinearGradientBrush(new Rectangle(36, 40, 5, 205), Color.FromArgb(70, 140, 255), Color.FromArgb(245, 35, 58), 90f);
        g.FillRectangle(rail, 36, 40, 5, 205);
        using var eyebrowFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        using var titleFont = new Font("Segoe UI Black", 64F, FontStyle.Bold | FontStyle.Italic);
        using var subFont = new Font("Segoe UI Black", 70F, FontStyle.Bold | FontStyle.Italic);
        using var eyebrowBrush = new SolidBrush(Color.FromArgb(115, 190, 255));
        using var titleBrush = new SolidBrush(Color.FromArgb(248, 250, 255));
        using var redBrush = new SolidBrush(Color.FromArgb(245, 42, 62));
        using var shadow = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        g.DrawString("THE NEXT GENERATION OF STREETBALL", eyebrowFont, eyebrowBrush, 54, 44);
        g.DrawString("QUBE", titleFont, shadow, 56, 91);
        g.DrawString("QUBE", titleFont, titleBrush, 51, 86);
        g.DrawString("2K23", subFont, shadow, 56, 151);
        g.DrawString("2K23", subFont, redBrush, 51, 146);

        using var tagFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        using var tagBrush = new SolidBrush(Color.FromArgb(205, 220, 242));
        using var tagBg = new SolidBrush(Color.FromArgb(95, 20, 42, 78));
        g.FillRectangle(tagBg, 53, 238, 290, 29);
        g.DrawString("PLAY  •  COMPETE  •  OWN THE COURT", tagFont, tagBrush, 64, 244);
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
                if (_gameExe is not null && File.Exists(_gameExe)) LaunchLocalGame();
                else MessageBox.Show("Choose your game folder or run a scan before launching.", "Game not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
            case "SETTINGS":
                ShowSettingsDialog();
                break;
            case "UPGRADES":
                ShowFeatureNotice("Upgrades", "The upgrades service is not connected yet. No purchases, player upgrades, or account changes are available in this launcher.");
                break;
            case "MYCAREER":
                ShowFeatureNotice("MyCAREER", "MyCAREER requires a supported game mode and connected game service. This launcher does not currently provide a MyCAREER backend.");
                break;
            case "STORE":
                ShowFeatureNotice("QUBE Store", "The store is not connected to a verified catalog or payment service. No purchases are available here.");
                break;
            case "SUPPORT":
                ShowSupportDialog();
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