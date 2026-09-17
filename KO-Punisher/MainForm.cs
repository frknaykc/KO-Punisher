using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm : Form
{
    private const int WmHotkey = 0x0312;
    private Settings _settings = new();
    private MacroEngine? _engine;
    private readonly WindowsMacroInput _input = new();
    private readonly HotkeyManager _hotkeyManager = new();
    private readonly SkillBarPanel _bar = new();
    private readonly Dictionary<string, TextBox> _keys = new();
    private readonly Dictionary<string, NumericUpDown> _nums = new();
    private readonly Dictionary<string, CheckBox> _checks = new();
    private readonly ComboBox _profiles = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 24 };
    private readonly ComboBox _runMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ComboBox _preset = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly CheckBox _jitter = new() { Text = "Jitter", AutoSize = true, ForeColor = UiTheme.Text };
    private readonly CheckBox _topMost = new() { Text = "Üstte", AutoSize = true, ForeColor = UiTheme.Text, Checked = true };
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Bg };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(10, 4, 10, 4), ForeColor = UiTheme.Muted };
    private readonly Button _start = UiTheme.NavButton("Başlat");
    private readonly Button _stop = UiTheme.NavButton("Durdur");
    private readonly TrackBar _opacity = new()
    {
        Minimum = 30, Maximum = 100, Value = 100, Width = 110, Height = 24,
        TickStyle = TickStyle.None, AutoSize = false
    };
    private readonly Label _opacityLbl = new() { Text = "100%", AutoSize = true, ForeColor = UiTheme.Muted, Padding = new Padding(4, 4, 8, 0) };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 120 };
    private readonly List<Button> _sideButtons = new();
    private readonly Panel _jobHost = new() { BackColor = UiTheme.Bg };
    private bool _closing, _allowClose, _applying;
    private string _page = "home";
    private string _jobTab = "atak";
    private string? _shownError;
    private CancellationTokenSource? _probeStop;
    private Task _probeTask = Task.CompletedTask;
    private Control? _homePage, _farmPage, _settingsPage, _ocrPage;
    private InventoryUpgradeControl? _upgradePage;
    private Panel? _atakPane, _minorPane, _yanPane, _digerPane, _asasAtakExtra;
    private FlowLayoutPanel? _palette;
    private readonly Dictionary<string, Label> _ocrCoords = new();
    private readonly ListBox _ocrLog = new()
    {
        Location = new Point(12, 210), Size = new Size(800, 280),
        BackColor = UiTheme.Slot, ForeColor = UiTheme.Text, BorderStyle = BorderStyle.FixedSingle
    };
    private readonly TextBox _ocrPhrase = new() { Location = new Point(12, 108), Width = 130, Text = "casting failed" };

    private readonly string? _settingsPath;

    public MainForm(string? settingsPath = null)
    {
        _settingsPath = settingsPath;
        string? loadError = null;
        try { _settings = Settings.Load(_settingsPath); _settings.RestoreJobDraft(); }
        catch (Exception ex) { loadError = ex.Message; }
        _settings.EnsureDefaultProfiles();
        BuildShell();
        // Bütün ayar kontrollerini yüklemeden önce oluştur; sonradan açılan
        // sekmeler boş tuş/minimum zamanlama ile kayıtlı ayarları ezmemeli.
        EnsurePanes();
        _farmPage = FarmPage();
        _ocrPage = OcrPage();
        _preset.FormattingEnabled = true;
        _preset.DropDownWidth = 340;
        _preset.Format += (_, e) => e.Value = ComboTitle(e.ListItem as string ?? "");
        _preset.SelectedIndexChanged += (_, _) => UpdateComboPanels();
        HandleCreated += (_, _) =>
        {
            _hotkeyManager.SetWindowHandle(Handle);
            _hotkeyManager.RegisterHotkeys(_settings);
            _hotkeyManager.HotKeyPressed += a =>
            {
                if (IsDisposed) return;
                BeginInvoke(() =>
                {
                    InputDiagnostics.Record("registered_hotkey", new { action = a });
                    if (a == "Start") Start();
                    else _ = StopAsync();
                });
            };
            _input.Configure(_settings);
            try { _input.InstallHooks(); }
            catch (Exception ex) { loadError = ex.Message; _status.Text = ex.Message; }
        };
        _status.Text = loadError ?? "Başlat → oyunu tıkla → tetik. Formu tıklama.";
        _timer.Tick += (_, _) => RefreshStatus();
        _timer.Start();
        ShowPage("home");
        ApplyToUi();
        InitializeComboHelp();
        UpdateComboPreview();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey) _hotkeyManager.ProcessHotkeyMessage(m.WParam.ToInt32());
        base.WndProc(ref m);
    }

    private void BuildShell()
    {
        Text = "KO-Punisher";
        UiTheme.PaintDark(this);
        ClientSize = new Size(1024, 620);
        MinimumSize = new Size(1024, 620);
        MaximumSize = new Size(1024, 620);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        FormClosing += OnClosing;
        try
        {
            string? exe = Environment.ProcessPath;
            if (exe != null) Icon = Icon.ExtractAssociatedIcon(exe);
        }
        catch { /* exe icon */ }

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.Panel,
            Padding = new Padding(8, 6, 8, 4), WrapContents = false
        };
        _topMost.CheckedChanged += (_, _) => TopMost = _topMost.Checked;
        top.Controls.Add(_topMost);
        top.Controls.Add(new Label { Text = "Şeffaf", AutoSize = true, ForeColor = UiTheme.Muted, Padding = new Padding(10, 4, 4, 0) });
        _opacity.ValueChanged += (_, _) =>
        {
            Opacity = _opacity.Value / 100.0;
            _opacityLbl.Text = _opacity.Value + "%";
        };
        top.Controls.Add(_opacity);
        top.Controls.Add(_opacityLbl);
        top.Controls.Add(new Label { Text = "Profil", AutoSize = true, ForeColor = UiTheme.Gold, Padding = new Padding(12, 4, 6, 0) });
        _profiles.BackColor = UiTheme.Slot;
        _profiles.ForeColor = UiTheme.Text;
        _profiles.FlatStyle = FlatStyle.Flat;
        _profiles.SelectedIndexChanged += (_, _) => OnProfilePicked();
        top.Controls.Add(_profiles);
        var add = UiTheme.NavButton("Oluştur");
        add.Click += (_, _) => AddProfile();
        var saveP = UiTheme.NavButton("Kaydet");
        saveP.Click += (_, _) => SaveProfile();
        top.Controls.Add(add);
        top.Controls.Add(saveP);
        _start.Click += (_, _) => Start();
        _stop.Click += async (_, _) => await StopAsync();
        _start.BackColor = UiTheme.Ok;
        top.Controls.Add(_start);
        top.Controls.Add(_stop);

        var side = new Panel { Dock = DockStyle.Left, Width = 168, BackColor = UiTheme.Panel };
        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, BackColor = UiTheme.Panel, Padding = new Padding(0, 4, 0, 0)
        };
        foreach (var (label, id) in new[]
        {
            ("Anasayfa", "home"), ("Assassin", "assassin"), ("Archery", "archery"),
            ("Warrior", "warrior"), ("Priest", "priest"), ("Mage / Restore", "mage"),
            ("Battle Priest", "bp"), ("Farm", "farm"), ("Upgrade", "upgrade"), ("OCR", "ocr"), ("Ayarlar", "settings"), ("Tanılama", "diagnostics")
        })
        {
            var b = UiTheme.SideButton(label);
            b.Dock = DockStyle.None;
            b.Width = 168;
            string page = id;
            b.Click += (_, _) => ShowPage(page);
            _sideButtons.Add(b);
            nav.Controls.Add(b);
        }
        side.Controls.Add(nav);
        if (AppAssets.Logo != null)
        {
            side.Controls.Add(new PictureBox
            {
                Image = AppAssets.Logo, Dock = DockStyle.Top, Height = 72,
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent
            });
        }

        Controls.Add(_content);
        Controls.Add(_status);
        Controls.Add(top);
        Controls.Add(side);
        BindKeyCapture();
    }

    private void ShowPage(string page)
    {
        if (_upgradePage?.Busy == true || _skillLayoutEditor?.Busy == true) return;
        if (page == "upgrade" && (_engine?.IsArmed == true || !_probeTask.IsCompleted)) return;
        if ((_engine?.IsArmed == true || !_probeTask.IsCompleted) &&
            page is "assassin" or "archery" or "warrior" or "priest" or "mage" or "bp")
        {
            _status.Text = "Job değiştirmeden önce Durdur.";
            return;
        }
        if (!_applying && _preset.Items.Count > 0 && _engine?.IsArmed != true && _probeTask.IsCompleted)
            _settings = ReadUiDraft();
        if (_settings.ClassType != JobForPage(page) && JobForPage(page) != null)
        {
            _settings.SwitchJob(JobForPage(page)!.Value);
            try { _settings.SaveJobDrafts(_settingsPath); }
            catch (Exception ex) { _status.Text = "Job taslağı kaydedilemedi: " + ex.Message; }
            ApplyToUi();
        }
        _page = page;
        HighlightSide(page);
        _content.Controls.Clear();
        Control body = page switch
        {
            "home" => _homePage ??= HomePage(),
            "farm" => _farmPage ??= FarmPage(),
            "ocr" => _ocrPage ??= OcrPage(),
            "upgrade" => _upgradePage ??= new InventoryUpgradeControl(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KO-Punisher", "inventory-crop.json"), () => _settings.TargetProcess), 
            "settings" => _settingsPage ??= SettingsPage(),
            "diagnostics" => _diagnosticsPage ??= DiagnosticsPage(),
            "assassin" or "archery" or "warrior" or "priest" or "mage" or "bp" => JobPageFor(page),
            _ => CompactHint(page)
        };
        body.Dock = DockStyle.Fill;
        _content.Controls.Add(body);
    }

    private void HighlightSide(string page)
    {
        string[] ids = ["home", "assassin", "archery", "warrior", "priest", "mage", "bp", "farm", "upgrade", "ocr", "settings", "diagnostics"];
        for (int i = 0; i < _sideButtons.Count && i < ids.Length; i++)
        {
            bool on = ids[i] == page;
            _sideButtons[i].BackColor = on ? UiTheme.KeyChip : UiTheme.Panel;
            _sideButtons[i].ForeColor = UiTheme.Text;
        }
    }

    private Control JobPageFor(string page)
    {
        ClassType job = page switch
        {
            "archery" => ClassType.Archer,
            "warrior" => ClassType.Warrior,
            "priest" => ClassType.Priest,
            "mage" => ClassType.Mage,
            "bp" => ClassType.BattlePriest,
            _ => ClassType.Assassin
        };
        _settings.ClassType = job;
        FillPresetItems(page);
        RebuildJobChrome(page);
        FillPalette(page);
        _skillLayoutEditor!.LoadLayout(job, _settings.SkillLayout);
        UpdateComboPanels();
        _jobTab = TabId(TabsFor(page)[0]);
        ShowJobTab(_jobTab);
        return _jobHost;
    }

    private void UpdateComboPanels()
    {
        bool slots = _page == "archery";
        if (_asasAtakExtra != null) _asasAtakExtra.Visible = !slots;
        if (!_applying) UpdateCalibratedKeys();
        UpdateComboGuide();
    }

    private static string[] TabsFor(string page) => page switch
    {
        "assassin" => ["Atak", "Minor", "Yan Skiller", "Diğer"],
        "archery" => ["Kombo", "Minor", "Diğer"],
        "warrior" => ["Atak", "Minor", "Diğer"],
        "priest" => ["Atak", "HP/MP", "Diğer"],
        "mage" => ["Atak", "Minor", "Diğer"],
        "bp" => ["Atak", "Heal", "Diğer"],
        _ => ["Atak", "Diğer"]
    };

    private static string TabId(string name) => name switch
    {
        "Yan Skiller" or "Skill bar" => "yan",
        "HP/MP" => "hp",
        "Heal" => "minor",
        "Kombo" => "atak",
        _ => name.ToLowerInvariant()
    };

    private void FillPresetItems(string page)
    {
        _preset.Items.Clear();
        object[] items = page switch
        {
            "archery" => [ComboCatalog.FiveSlideThreeSlide, ComboCatalog.ThreeSlideFiveSlide, "5-3", "3-5", "70-72", "70-60", "70-72-60"],
            "mage" => ["staff-r", "rr", "r"],
            "bp" => ["bp-rr", "r"],
            "priest" => ["rr", "r"],
            "warrior" => ["rotation", "rr"],
            _ => ["rotation", "assassin-skill-r", "assassin-rr", "assassin-r-skill"]
        };
        _preset.Items.AddRange(items);
        string want = _settings.ComboPreset;
        int i = _preset.Items.IndexOf(want);
        _preset.SelectedIndex = i >= 0 ? i : 0;
    }

    private void RebuildJobChrome(string page)
    {
        _jobHost.Controls.Clear();
        EnsurePanes();
        var tabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 36, Padding = new Padding(8, 6, 8, 0),
            WrapContents = false, BackColor = UiTheme.Bg
        };
        foreach (string name in TabsFor(page).Concat(new[] { "Skill Bar" }))
        {
            var b = UiTheme.NavButton(name);
            string id = TabId(name);
            b.Click += (_, _) => ShowJobTab(id);
            tabs.Controls.Add(b);
        }
        var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Bg };
        foreach (var pane in new[] { _atakPane!, _minorPane!, _yanPane!, _digerPane! })
        {
            pane.Parent = null;
            pane.Dock = DockStyle.Fill;
            body.Controls.Add(pane);
        }
        EnsureSkillLayoutEditor();
        _skillLayoutEditor!.Parent = null;
        _skillLayoutEditor.Dock = DockStyle.Fill;
        body.Controls.Add(_skillLayoutEditor);
        _jobHost.Controls.Add(body);
        _jobHost.Controls.Add(tabs);
    }

    private void EnsurePanes()
    {
        _atakPane ??= AtakPane();
        _minorPane ??= MinorPane();
        _yanPane ??= YanPane();
        _digerPane ??= DigerPane();
    }

    private void ShowJobTab(string tab)
    {
        _jobTab = tab;
        if (_skillLayoutEditor != null) _skillLayoutEditor.Visible = tab == "skill bar";
        if (_atakPane == null) return;
        _atakPane.Visible = tab is "atak";
        _minorPane!.Visible = tab is "minor" or "hp";
        _yanPane!.Visible = tab == "yan";
        _digerPane!.Visible = tab is "diğer" or "diger";
    }

    private Panel AtakPane()
    {
        var p = new Panel { BackColor = UiTheme.Bg, AutoScroll = true };
        int y = 10;
        Add(p, Lbl("Atak tuş", 12, y));
        Add(p, KeyBox("ComboTrigger", 12, y + 16));
        _runMode.Items.Clear();
        _runMode.Items.AddRange(new object[] { "Basılı tut", "Toggle" });
        _runMode.Location = new Point(150, y + 16);
        _runMode.BackColor = UiTheme.Slot;
        _runMode.ForeColor = UiTheme.Text;
        Add(p, _runMode);
        Add(p, Lbl("Kombo", 330, y));
        _preset.Location = new Point(330, y + 16);
        _preset.BackColor = UiTheme.Slot;
        _preset.ForeColor = UiTheme.Text;
        Add(p, _preset);
        AddRuntimeControls(p);
        Add(p, Lbl("Adım arası (ms)", 500, y));
        Add(p, Num("ComboSpeedMs", 500, y + 16, 50, 2000));

        _asasAtakExtra = new Panel { Location = new Point(0, 48), Size = new Size(840, 220), BackColor = UiTheme.Bg };
        for (int i = 0; i < _comboKeyLabels.Length; i++)
        {
            _comboKeyLabels[i].Location = new Point(12 + i * 220, 268);
            Add(p, _comboKeyLabels[i]);
            Add(p, KeyBox("ComboKey" + (i + 1), 12 + i * 220, 288));
        }
        Add(p, _comboGuide);
        Add(_asasAtakExtra, Chk("WsCombo", "Skill → Slide → R", 12, 54));
        Add(p, Lbl("Hareket tuşu", 680, 268));
        Add(p, KeyBox("SlideKey", 680, 288));
        Add(_asasAtakExtra, Lbl("LightFeet", 400, 52));
        Add(_asasAtakExtra, KeyBox("LightFeetKey", 400, 68));
        Add(_asasAtakExtra, Lbl("Insert tetik (Stix/Cure/M20)", 12, 108));
        Add(_asasAtakExtra, KeyBox("InsertTrigger", 12, 124));
        Add(_asasAtakExtra, Lbl("Insert tuş", 150, 108));
        Add(_asasAtakExtra, KeyBox("InsertKey", 150, 124));
        Add(_asasAtakExtra, Lbl("HP zorla tetik", 288, 108));
        Add(_asasAtakExtra, KeyBox("ForceHpTrigger", 288, 124));
        p.Controls.Add(_asasAtakExtra);

        p.Controls.Add(_comboPreview);
        return p;
    }

    private Panel MinorPane()
    {
        var p = new Panel { BackColor = UiTheme.Bg };
        Add(p, Lbl("Tetik", 12, 10));
        Add(p, KeyBox("MinorTrigger", 12, 26));
        Add(p, Lbl("Minor tuş", 150, 10));
        Add(p, KeyBox("MinorPedalKey", 150, 26));
        Add(p, Lbl("Hold ms", 288, 10));
        Add(p, Num("MinorHoldMs", 288, 26, 5, 1000));
        Add(p, Lbl("Tekrar ms", 430, 10));
        Add(p, Num("MinorRepeatMs", 430, 26, 10, 5000));
        Add(p, Lbl("Pot", 12, 64));
        Add(p, KeyBox("MinorPotKey", 12, 80));
        Add(p, Lbl("Mana", 150, 64));
        Add(p, KeyBox("MinorManaKey", 150, 80));
        Add(p, Lbl("Acil", 288, 64));
        Add(p, KeyBox("EmergencyStop", 288, 80));
        return p;
    }

    private Panel YanPane()
    {
        var p = new Panel { BackColor = UiTheme.Bg };
        _palette = new FlowLayoutPanel
        {
            Location = new Point(8, 8), Size = new Size(820, 130),
            BackColor = UiTheme.PanelAlt, WrapContents = true
        };
        _bar.Location = new Point(8, 144);
        _bar.Width = 820;
        p.Controls.Add(_palette);
        p.Controls.Add(_bar);
        return p;
    }

    private void FillPalette(string page)
    {
        EnsurePanes();
        var catalog = page == "archery" ? SkillCatalog.Archer : SkillCatalog.Assassin;
        _bar.ActiveCatalog = catalog;
        if (_palette == null) return;
        _palette.Controls.Clear();
        foreach (var skill in catalog)
            _palette.Controls.Add(new SkillIconTile(skill));
    }

    private Panel DigerPane()
    {
        var p = new Panel { BackColor = UiTheme.Bg, AutoScroll = true };
        Add(p, Lbl("Skill basışı (ms)", 12, 10));
        Add(p, Num("SkillKeyHold", 12, 26, 5, 2000));
        Add(p, Lbl("Skill sonrası (ms)", 150, 10));
        Add(p, Num("SkillToWDelay", 150, 26, 0, 2000));
        Add(p, Lbl("Hareket basışı (ms)", 288, 10));
        Add(p, Num("WKeyHold", 288, 26, 5, 2000));
        Add(p, Lbl("R öncesi (ms)", 430, 10));
        Add(p, Num("WToRDelay", 430, 26, 0, 2000));
        Add(p, Lbl("R basışı (ms)", 12, 64));
        Add(p, Num("RKeyHold", 12, 80, 5, 2000));
        Add(p, Lbl("Tur aralığı (ms)", 150, 64));
        Add(p, Num("NextSkillDelay", 150, 80, 5, 2000));
        _jitter.Location = new Point(288, 80);
        Add(p, _jitter);
        Add(p, Lbl("Jitter ms", 400, 64));
        Add(p, Num("JitterRange", 400, 80, 0, 50));
        AddArcherTimingControls(p);
        return p;
    }

    private Control HomePage()
    {
        var p = new Panel { BackColor = UiTheme.Bg };
        var art = new PictureBox
        {
            Dock = DockStyle.Right, Width = 280, SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = UiTheme.Bg, Image = AppAssets.HomeArt
        };
        var left = new Panel { Dock = DockStyle.Fill };
        if (AppAssets.Logo != null)
        {
            left.Controls.Add(new PictureBox
            {
                Image = AppAssets.Logo, Location = new Point(16, 12), Size = new Size(240, 160),
                SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent
            });
        }
        left.Controls.Add(new Label
        {
            Text = "Soldan job seç. Atak sekmesinde tetik ve kombo.\nBaşlat, oyunu tıkla, tetik bas. Formu tıklama.",
            Location = new Point(16, 180), AutoSize = true, ForeColor = UiTheme.Muted
        });
        p.Controls.Add(left);
        p.Controls.Add(art);
        return p;
    }

    private Control OcrPage()
    {
        var p = new Panel { BackColor = UiTheme.Bg };
        Add(p, Lbl("Kırp: Skill Bar, Chat, HP/MP, Status. Casting failed Status’te aranır. Oyun belleği okunmaz.", 12, 8));
        int x = 12;
        foreach (var (name, getter) in new (string, Func<UiRegion>)[]
        {
            ("SkillBar", () => _settings.OcrSkillBar),
            ("Chat", () => _settings.OcrChat),
            ("HpMp", () => _settings.OcrHpMp),
            ("Status", () => _settings.OcrStatus)
        })
        {
            var b = UiTheme.NavButton(name + " çiz");
            b.Location = new Point(x, 32);
            string id = name;
            b.Click += (_, _) => PickOcr(id);
            p.Controls.Add(b);
            var lab = new Label { Location = new Point(x, 66), AutoSize = true, ForeColor = UiTheme.Muted, Text = getter().ToString() };
            _ocrCoords[id] = lab;
            p.Controls.Add(lab);
            x += 160;
        }
        Add(p, Lbl("Fail metni", 12, 92));
        UiTheme.StyleField(_ocrPhrase);
        p.Controls.Add(_ocrPhrase);
        Add(p, Lbl("Tur", 150, 92));
        Add(p, Num("OcrTestCycles", 150, 108, 1, 10));
        var go = UiTheme.NavButton("2 sn sonra 3-5-W dene");
        go.Location = new Point(250, 106);
        go.Click += async (_, _) => await RunOcrProbeAsync();
        p.Controls.Add(go);
        Add(p, Lbl("Oyunu tıkla, countdown bitince 5→W→3→W basılır; Status OCR damgalanır.", 12, 144));
        p.Controls.Add(_ocrLog);
        return p;
    }

    private void PickOcr(string id)
    {
        using var overlay = new OcrOverlay();
        if (overlay.ShowDialog(this) != DialogResult.OK) return;
        var r = overlay.ScreenRect;
        UiRegion box = id switch
        {
            "SkillBar" => _settings.OcrSkillBar,
            "Chat" => _settings.OcrChat,
            "HpMp" => _settings.OcrHpMp,
            _ => _settings.OcrStatus
        };
        box.X = r.X; box.Y = r.Y; box.W = r.Width; box.H = r.Height;
        if (_ocrCoords.TryGetValue(id, out var lab)) lab.Text = box.ToString();
        _status.Text = id + " kırpıldı: " + box;
    }

    private async Task RunOcrProbeAsync()
    {
        if (_closing || !_probeTask.IsCompleted || _engine?.IsArmed == true) return;
        using var stop = new CancellationTokenSource();
        _probeStop = stop;
        try
        {
            _probeTask = RunOcrProbeCoreAsync(stop.Token);
            await _probeTask;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        { _status.Text = "OCR testi durduruldu."; }
        catch (Exception ex) { _status.Text = "OCR testi başarısız: " + ex.Message; }
        finally
        {
            _probeStop = null;
            try { InputSender.CleanupAllKeys(); }
            catch (Exception ex) { _status.Text = "Tuş bırakma hatası: " + ex.Message; }
        }
    }

    private async Task RunOcrProbeCoreAsync(CancellationToken token)
    {
        if (!_settings.OcrStatus.IsSet)
        {
            _status.Text = "Önce Status alanını çiz.";
            return;
        }
        _ocrLog.Items.Clear();
        try { _settings = ReadUi(); }
        catch (Exception ex)
        {
            _status.Text = "OCR testi başlatılamadı: " + ex.Message;
            return;
        }
        for (int i = 2; i >= 1; i--)
        {
            _status.Text = $"OCR test {i} sn — oyunu tıkla, formu tıklama.";
            await Task.Delay(1000, token);
        }
        if (!ComboCatalog.IsSlide(_settings.ComboPreset))
            throw new InvalidOperationException("OCR combo testi için hareketli okçu combosunu seç.");
        var steps = ComboCatalog.SlideSteps(_settings);
        var status = new Rectangle(_settings.OcrStatus.X, _settings.OcrStatus.Y, _settings.OcrStatus.W, _settings.OcrStatus.H);
        string phrase = _settings.OcrFailPhrase;
        int fails = 0;
        for (int c = 1; c <= _settings.OcrTestCycles; c++)
        {
            LogOcr($"--- tur {c} · {ComboTitle(_settings.ComboPreset)} ---");
            foreach (var (key, h, after) in steps)
            {
                long t = Environment.TickCount64;
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (!_input.IsTargetForeground(_settings.TargetProcess)) throw new InvalidOperationException("Oyun odağı kayboldu; OCR testi iptal edildi.");
                    InputSender.KeyDown(key);
                    await WaitForGameAsync(h, token);
                }
                finally { InputSender.KeyUp(key); }
                LogOcr($"{t} KEY {key} hold={h}");
                await WaitForGameAsync(after, token);
            }
            // OCR tur arasına gecikme ekler; sürekli combo zamanlaması testi değildir.
            string text = await OcrReader.ReadAsync(status, token);
            token.ThrowIfCancellationRequested();
            if (OcrReader.LooksFailed(text, phrase)) fails++;
            LogOcr($"Tur {c} OCR: {TrimOcr(text)} (tur arası OCR beklemesi var)");
        }
        _status.Text = fails == 0
            ? "OCR test: hata metni görülmedi; oyun kabulü doğrulanmadı."
            : $"OCR test: {fails} turda hata metni görüldü; bu sayı ayrı hata sayısı değildir.";
    }

    private void LogOcr(string line)
    {
        _ocrLog.Items.Add(line);
        _ocrLog.TopIndex = Math.Max(0, _ocrLog.Items.Count - 1);
    }

    private static string TrimOcr(string t)
    {
        t = (t ?? "").Replace('\n', ' ').Trim();
        return t.Length <= 80 ? t : t[..80];
    }

    private Control FarmPage()
    {
        var p = new Panel { BackColor = UiTheme.Bg, Size = new Size(820, 520) };
        Add(p, Lbl("Hedef Z", 12, 12));
        Add(p, KeyBox("ZKey", 12, 28));
        Add(p, Lbl("Saldırı", 150, 12));
        Add(p, KeyBox("ZAttackKey", 150, 28));
        Add(p, Lbl("Aralık ms", 288, 12));
        Add(p, Num("ZInterval", 288, 28, 500, 30000));
        Add(p, Lbl("Atak açıkken Z+saldırı basılır. Mob filtresi açıksa tüm saldırı çıkışları kontrol edilir.", 12, 70));
        AddFarmControls(p);
        return p;
    }

    private Control SettingsPage()
    {
        var p = new Panel { BackColor = UiTheme.Bg };
        Add(p, Lbl(Settings.FilePath, 12, 12));
        Add(p, Lbl("HP eşiği oyun belleğinden okunmaz. Pot/mana tuşu + HP zorla tetik kullan.", 12, 36));
        Add(p, Lbl("Üst çubuktaki Şeffaf kaydırıcı pencere opaklığını ayarlar (30–100%).", 12, 60));
        return p;
    }

    private static Control CompactHint(string page)
    {
        var p = new Panel { BackColor = UiTheme.Bg };
        p.Controls.Add(new Label { Text = page + " — yakında", Location = new Point(16, 16), AutoSize = true, ForeColor = UiTheme.Gold });
        return p;
    }

    private static void Add(Control parent, Control child) => parent.Controls.Add(child);

    private CheckBox Chk(string name, string text, int x, int y)
    {
        if (_checks.TryGetValue(name, out var existing))
        {
            existing.Location = new Point(x, y);
            return existing;
        }
        var box = new CheckBox { Name = name, Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = UiTheme.Text, Checked = true };
        _checks[name] = box;
        return box;
    }

    private static Label Lbl(string t, int x, int y) =>
        new() { Text = t, Location = new Point(x, y), AutoSize = true, ForeColor = UiTheme.Muted };

    private TextBox KeyBox(string name, int x, int y)
    {
        if (_keys.TryGetValue(name, out var existing))
        {
            existing.Location = new Point(x, y);
            return existing;
        }
        var box = new TextBox { Name = name, Location = new Point(x, y), Width = 110, MaxLength = 16 };
        UiTheme.StyleField(box);
        _keys[name] = box;
        return box;
    }

    private NumericUpDown Num(string name, int x, int y, int min, int max)
    {
        if (_nums.TryGetValue(name, out var existing))
        {
            existing.Location = new Point(x, y);
            existing.Minimum = min;
            existing.Maximum = max;
            return existing;
        }
        var box = new NumericUpDown { Name = name, Location = new Point(x, y), Width = 80, Minimum = min, Maximum = max };
        UiTheme.StyleNumeric(box);
        _nums[name] = box;
        return box;
    }

    private void BindKeyCapture() => _content.ControlAdded += AttachCapture;

    private void AttachCapture(object? sender, ControlEventArgs e)
    {
        if (e.Control != null) Walk(e.Control);
    }

    private void Walk(Control c)
    {
        if (c is TextBox tb && _keys.ContainsValue(tb) && tb.Tag as string != "cap")
        {
            tb.Tag = "cap";
            tb.KeyDown += (_, e) =>
            {
                tb.Text = e.KeyCode switch
                {
                    Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => "CTRL",
                    Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => "SHIFT",
                    Keys.Menu or Keys.LMenu or Keys.RMenu => "ALT",
                    >= Keys.D0 and <= Keys.D9 => ((int)e.KeyCode - (int)Keys.D0).ToString(),
                    Keys.Capital => "CAPSLOCK",
                    Keys.Back or Keys.Delete => "",
                    _ => e.KeyCode.ToString().ToUpperInvariant()
                };
                e.SuppressKeyPress = true;
            };
        }
        foreach (Control child in c.Controls) Walk(child);
        c.ControlAdded -= AttachCapture;
        c.ControlAdded += AttachCapture;
    }

    private void ApplyToUi()
    {
        _applying = true;
        try
        {
            _profiles.Items.Clear();
            foreach (string n in _settings.GetProfileNames()) _profiles.Items.Add(n);
            int i = _profiles.Items.IndexOf(_settings.ActiveProfile);
            if (i >= 0) _profiles.SelectedIndex = i;
            else if (_profiles.Items.Count > 0) _profiles.SelectedIndex = 0;
            SetKey("ComboTrigger", _settings.ComboTrigger);
            SetKey("MinorTrigger", _settings.MinorTrigger);
            SetKey("EmergencyStop", _settings.Hotkeys.EmergencyStop);
            SetKey("MinorPedalKey", _settings.MinorPedalKey);
            SetKey("MinorPotKey", _settings.MinorPotKey);
            SetKey("MinorManaKey", _settings.MinorManaKey);
            SetKey("ZKey", _settings.ZKey);
            SetKey("ZAttackKey", _settings.ZAttackKey);
            SetKey("SlideKey", _settings.SlideKey);
            SetKey("ComboKey1", _settings.ComboKey1);
            SetKey("ComboKey2", _settings.ComboKey2);
            SetKey("ComboKey3", _settings.ComboKey3);
            SetKey("InsertTrigger", _settings.InsertTrigger);
            SetKey("InsertKey", _settings.InsertKey);
            SetKey("ForceHpTrigger", _settings.ForceHpTrigger);
            SetKey("LightFeetKey", _settings.LightFeetKey);
            SetNum("MinorHoldMs", _settings.MinorHoldMs);
            SetNum("MinorRepeatMs", _settings.MinorRepeatMs);
            SetNum("ArcherSkillHold", _settings.Timings.ArcherSkillHold);
            SetNum("ArcherSkillAfter", _settings.Timings.ArcherSkillAfter);
            SetNum("ArcherSlideHold", _settings.Timings.ArcherSlideHold);
            SetNum("ArcherSlideAfter", _settings.Timings.ArcherSlideAfter);
            SetNum("ArcherCycleAfter", _settings.Timings.ArcherCycleAfter);
            SetNum("SkillKeyHold", _settings.Timings.SkillKeyHold);
            SetNum("SkillToWDelay", _settings.Timings.SkillToWDelay);
            SetNum("WKeyHold", _settings.Timings.WKeyHold);
            SetNum("WToRDelay", _settings.Timings.WToRDelay);
            SetNum("RKeyHold", _settings.Timings.RKeyHold);
            SetNum("NextSkillDelay", _settings.Timings.NextSkillDelay);
            SetNum("JitterRange", _settings.JitterRange);
            SetNum("ZInterval", _settings.ZInterval);
            SetNum("ComboSpeedMs", _settings.ComboSpeedMs);
            _ocrPhrase.Text = _settings.OcrFailPhrase;
            SetNum("OcrTestCycles", _settings.OcrTestCycles);
            int op = Math.Clamp(_settings.WindowOpacity, 30, 100);
            _opacity.Value = op;
            Opacity = op / 100.0;
            _opacityLbl.Text = op + "%";
            _jitter.Checked = _settings.EnableJitter;
            if (_runMode.Items.Count == 0)
                _runMode.Items.AddRange(new object[] { "Basılı tut", "Toggle" });
            _runMode.SelectedIndex = _settings.RunMode == RunMode.Toggle ? 1 : 0;
            if (_preset.Items.Count > 0)
            {
                int pi = _preset.Items.IndexOf(_settings.ComboPreset);
                if (pi >= 0) _preset.SelectedIndex = pi;
            }
            if (_checks.TryGetValue("WsCombo", out var ws))
                ws.Checked = _settings.SkillRSkillMode == SkillRSkillMode.None;
            ApplyFarmToUi();
            _bar.LoadFrom(_settings);
            _skillLayoutEditor?.LoadLayout(_settings.ClassType, _settings.SkillLayout);
        }
        finally { _applying = false; }
    }

    private void SetKey(string n, string v)
    {
        if (_keys.TryGetValue(n, out var b)) b.Text = v;
    }

    private void SetNum(string n, int v)
    {
        if (!_nums.TryGetValue(n, out var b)) return;
        b.Value = Math.Clamp(v, (int)b.Minimum, (int)b.Maximum);
    }

    private string KeyVal(string n) => _keys.TryGetValue(n, out var b) ? b.Text : "";
    private string K(string n, string cur) => _keys.ContainsKey(n) ? KeyVal(n) : cur;
    private int NumVal(string n) => _nums.TryGetValue(n, out var b) ? (int)b.Value : 0;
    private int Fallback(string n, int cur) => _nums.ContainsKey(n) ? NumVal(n) : cur;

    private Settings ReadUi() => ReadUiDraft(validate: true);

    private Settings ReadUiDraft(bool validate = false)
    {
        var s = new Settings
        {
            ComboTrigger = K("ComboTrigger", _settings.ComboTrigger),
            MinorTrigger = K("MinorTrigger", _settings.MinorTrigger),
            MinorPedalKey = K("MinorPedalKey", _settings.MinorPedalKey),
            MinorPotKey = K("MinorPotKey", _settings.MinorPotKey),
            MinorManaKey = K("MinorManaKey", _settings.MinorManaKey),
            ZKey = K("ZKey", _settings.ZKey),
            ZAttackKey = K("ZAttackKey", _settings.ZAttackKey),
            SlideKey = K("SlideKey", _settings.SlideKey),
            ComboKey1 = K("ComboKey1", _settings.ComboKey1),
            ComboKey2 = K("ComboKey2", _settings.ComboKey2),
            ComboKey3 = K("ComboKey3", _settings.ComboKey3),
            InsertTrigger = K("InsertTrigger", _settings.InsertTrigger),
            InsertKey = K("InsertKey", _settings.InsertKey),
            ForceHpTrigger = K("ForceHpTrigger", _settings.ForceHpTrigger),
            LightFeetKey = K("LightFeetKey", _settings.LightFeetKey),
            ComboPreset = _preset.SelectedItem as string ?? _settings.ComboPreset,
            ComboSpeedMs = Fallback("ComboSpeedMs", _settings.ComboSpeedMs),
            WindowOpacity = _opacity.Value,
            OcrSkillBar = _settings.OcrSkillBar,
            OcrChat = _settings.OcrChat,
            OcrHpMp = _settings.OcrHpMp,
            OcrStatus = _settings.OcrStatus,
            OcrFailPhrase = string.IsNullOrWhiteSpace(_ocrPhrase.Text) ? _settings.OcrFailPhrase : _ocrPhrase.Text,
            OcrTestCycles = Fallback("OcrTestCycles", _settings.OcrTestCycles),
            ClassType = _settings.ClassType,
            SkillLayout = _settings.SkillLayout?.Clone(),
            MinorHoldMs = NumVal("MinorHoldMs") is 0 ? _settings.MinorHoldMs : NumVal("MinorHoldMs"),
            MinorRepeatMs = NumVal("MinorRepeatMs") is 0 ? _settings.MinorRepeatMs : NumVal("MinorRepeatMs"),
            Hotkeys = new Hotkeys
            {
                Start = _settings.Hotkeys.Start,
                Stop = _settings.Hotkeys.Stop,
                EmergencyStop = K("EmergencyStop", _settings.Hotkeys.EmergencyStop)
            },
            Timings = new Timings
            {
                ArcherSkillHold = Fallback("ArcherSkillHold", _settings.Timings.ArcherSkillHold),
                ArcherSkillAfter = Fallback("ArcherSkillAfter", _settings.Timings.ArcherSkillAfter),
                ArcherSlideHold = Fallback("ArcherSlideHold", _settings.Timings.ArcherSlideHold),
                ArcherSlideAfter = Fallback("ArcherSlideAfter", _settings.Timings.ArcherSlideAfter),
                ArcherCycleAfter = Fallback("ArcherCycleAfter", _settings.Timings.ArcherCycleAfter),
                SkillKeyHold = Fallback("SkillKeyHold", _settings.Timings.SkillKeyHold),
                SkillToWDelay = Fallback("SkillToWDelay", _settings.Timings.SkillToWDelay),
                WKeyHold = Fallback("WKeyHold", _settings.Timings.WKeyHold),
                WToRDelay = Fallback("WToRDelay", _settings.Timings.WToRDelay),
                RKeyHold = Fallback("RKeyHold", _settings.Timings.RKeyHold),
                NextSkillDelay = Fallback("NextSkillDelay", _settings.Timings.NextSkillDelay)
            },
            ZInterval = Fallback("ZInterval", _settings.ZInterval),
            JitterRange = Fallback("JitterRange", _settings.JitterRange),
            EnableJitter = _jitter.Checked,
            RunMode = _runMode.SelectedIndex == 1 ? RunMode.Toggle : RunMode.Hold,
            SkillRSkillMode = _checks.TryGetValue("WsCombo", out var ws) && !ws.Checked
                ? SkillRSkillMode.SkillR : SkillRSkillMode.None,
            TargetProcess = _settings.TargetProcess,
            Farm = ReadFarmUi(),
            JobDrafts = Settings.Snapshot(_settings.JobDrafts),
            Profiles = _settings.Profiles,
            ActiveProfile = _settings.ActiveProfile,
            Skills = new Dictionary<string, string>(_settings.Skills),
            FillerOrder = new List<string>(_settings.FillerOrder),
            SkillCooldowns = new Dictionary<string, int>(_settings.SkillCooldowns),
            Cooldowns = Settings.Snapshot(_settings.Cooldowns)
        };
        if (string.IsNullOrWhiteSpace(s.ComboTrigger)) s.ComboTrigger = _settings.ComboTrigger;
        if (string.IsNullOrWhiteSpace(s.MinorTrigger)) s.MinorTrigger = _settings.MinorTrigger;
        if (_page == "archery")
            s.ComboPreset = (_preset.SelectedItem as string) ?? ComboCatalog.FiveSlideThreeSlide;
        bool archer35 = s.ComboPreset is ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w";
        if (archer35 && s.ComboKey1.Length == 0 && s.ComboKey2.Length == 0)
        {
            s.ExclusiveBind("ArrowShower", "5");
            s.ExclusiveBind("MultipleShot", "3");
        }
        else
            _bar.ApplyTo(s);
        SkillCalibration.Apply(s, requireComplete: validate);
        if (validate) s.Validate();
        return s;
    }

    private void Start()
    {
        if (_skillLayoutEditor?.Busy == true) return;
        if (_page == "upgrade" || _upgradePage?.Busy == true) { _status.Text = "Makro için Upgrade sayfasından çık."; return; }
        if (_closing || !_probeTask.IsCompleted || (_engine != null && !_engine.Completion.IsCompleted)) return;
        try
        {
            _settings = ReadUi();
            _input.Configure(_settings);
            _input.InstallHooks();
            _hotkeyManager.RegisterHotkeys(_settings);
            _input.SetArmed(true);
            _engine = CreateEngine();
            _engine.Arm();
            _shownError = null;
            _status.Text = $"Hazır [{ComboTitle(_settings.ComboPreset)}] tetik {_settings.ComboTrigger}. Oyunu tıkla, tetik bas. Formu tıklama.";
        }
        catch (Exception ex)
        {
            _input.SetArmed(false);
            _status.Text = "Başlatılamadı: " + ex.Message;
            InputDiagnostics.Record("start_error", new { error = ex.Message });
        }
    }

    private async Task StopAsync()
    {
        InputDiagnostics.Record("stop_requested");
        _probeStop?.Cancel();
        try { await _probeTask; }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = "OCR testi durdu: " + ex.Message; }
        if (_engine != null) await _engine.StopAsync();
        _input.SetArmed(false);
        RefreshStatus();
    }

    private void SaveProfile()
    {
        if (_engine?.IsArmed == true || !_probeTask.IsCompleted) { _status.Text = "Ayarları kaydetmeden önce Durdur."; return; }
        try
        {
            var s = ReadUi();
            s.SaveCurrentToProfile();
            s.StoreJobDraft();
            s.Save(_settingsPath);
            _settings = s;
            _input.Configure(_settings);
            if (IsHandleCreated) _hotkeyManager.RegisterHotkeys(_settings);
            _status.Text = "Profil kaydedildi: " + _settings.ActiveProfile;
        }
        catch (Exception ex) { _status.Text = "Kaydedilemedi: " + ex.Message; }
    }

    private void AddProfile()
    {
        if (_engine?.IsArmed == true || !_probeTask.IsCompleted) { _status.Text = "Profil eklemeden önce Durdur."; return; }
        string name = _settings.ClassType + " " + (_settings.Profiles.Count + 1);
        try
        {
            _settings = ReadUi();
            _settings.AddProfile(name, _settings.ClassType);
            ApplyToUi();
            _status.Text = "Profil eklendi: " + name;
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private void OnProfilePicked()
    {
        if (_applying || _engine?.IsArmed == true || !_probeTask.IsCompleted) return;
        string? name = _profiles.SelectedItem as string;
        if (string.IsNullOrEmpty(name)) return;
        try
        {
            var s = ReadUiDraft();
            s.SaveCurrentToProfile();
            s.StoreJobDraft();
            s.ApplyProfile(name);
            s.RestoreJobDraft();
            _settings = s;
            string page = s.ClassType switch
            {
                ClassType.Archer => "archery", ClassType.Warrior => "warrior",
                ClassType.Priest => "priest", ClassType.Mage => "mage",
                ClassType.BattlePriest => "bp", _ => "assassin"
            };
            _applying = true;
            try { ShowPage(page); }
            finally { _applying = false; }
            ApplyToUi();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private void RefreshStatus()
    {
        SampleDiagnostics();
        bool armed = _engine?.IsArmed == true;
        bool probing = !_probeTask.IsCompleted || _skillLayoutEditor?.Busy == true;
        _start.Enabled = !armed && !probing && !_closing && _page != "upgrade";
        _stop.Enabled = (armed || probing) && !_closing;
        _profiles.Enabled = !armed && !probing;
        foreach (var button in _sideButtons) button.Enabled = !armed && !probing;
        if (_engine?.Error is string err && err != _shownError)
        {
            _shownError = err;
            _status.Text = "Durduruldu: " + err;
        }
        foreach (var control in _keys.Values.Cast<Control>().Concat(_nums.Values).Concat(_checks.Values))
            control.Enabled = !armed && !probing;
        _preset.Enabled = _runMode.Enabled = !armed && !probing;
        if (_skillLayoutEditor != null) _skillLayoutEditor.Enabled = !armed && !probing;
        if (!armed && !probing) UpdateComboGuide();
        if (_farmPage != null) _farmPage.Enabled = !armed && !probing;
        if (_marketRunner != null && probing) _status.Text = $"Pazar: {_marketRunner.SentCount}/{_settings.Farm.Market.RepeatCount} · Durdur: {_settings.Hotkeys.Stop}";
        if (armed && _settings.Farm.Monster.Enabled) _monsterResult.Text = _engine!.MonsterStatus;
        RefreshOverlay(armed, probing);
        if (!armed) return;
        if (_engine!.FocusPaused)
        {
            _status.Text = "Duraklatıldı: oyun ön planda değil. Dönüşte tetiği bırakıp yeniden bas.";
            return;
        }
        string combo = _engine!.ComboActive ? "atak açık" : "atak bekliyor";
        string minor = _engine.MinorActive ? "minor açık" : "minor bekliyor";
        _status.Text = $"{_settings.ClassType} [{_settings.ComboTrigger}]: {combo}  ·  Minor [{_settings.MinorTrigger}]: {minor}  ·  {ComboTitle(_settings.ComboPreset)}";
        if (_settings.Farm.Monster.Enabled) _status.Text += " · " + _engine.MonsterStatus;
    }

    private async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose) return;
        if (_skillLayoutEditor?.Busy == true) { e.Cancel = true; return; }
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        _timer.Stop();
        await FinishDiagnosticsAsync();
        try { ReadUiDraft().SaveJobDrafts(_settingsPath); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Job taslakları kaydedilemedi"); }
        _upgradePage?.Dispose();
        _overlay?.Dispose();
        _hotkeyManager.Dispose();
        _input.Dispose();
        _timer.Dispose();
        _help.Dispose();
        _allowClose = true;
        Close();
    }
}
