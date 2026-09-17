using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm
{
    private readonly CheckBox[] _buffEnabled = Enumerable.Range(0, 8).Select(_ => new CheckBox { AutoSize = true, ForeColor = UiTheme.Text }).ToArray();
    private readonly CheckBox _monsterEnabled = new() { Text = "Yalnız listedeki mob isimlerine saldır", AutoSize = true, ForeColor = UiTheme.Text };
    private readonly TextBox _monsterNames = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Size = new Size(300, 165) };
    private readonly Label _monsterRegion = new() { AutoSize = true, ForeColor = UiTheme.Muted };
    private readonly Label _monsterResult = new() { Size = new Size(420, 90), ForeColor = UiTheme.Text };
    private readonly TextBox _marketMessages = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Size = new Size(710, 145) };
    private readonly CheckBox _chatClosed = new() { Text = "Oyun sohbeti kapalı, Caps Lock kapalı; gönderim sırasında klavyeyi kullanmayacağım.", AutoSize = true, ForeColor = UiTheme.Text };
    private string? _farmAction;
    private MarketRunner? _marketRunner;

    private void AddFarmControls(Control parent)
    {
        var tabs = new TabControl { Location = new Point(12, 94), Size = new Size(790, 395), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        var buffs = new TabPage("Buff zamanlayıcı") { BackColor = UiTheme.Bg };
        var monster = new TabPage("Mob filtresi") { BackColor = UiTheme.Bg };
        var market = new TabPage("Pazar mesajları") { BackColor = UiTheme.Bg };
        tabs.TabPages.AddRange([buffs, monster, market]);
        parent.Controls.Add(tabs);

        Add(buffs, Lbl("Etkin / skill", 12, 12));
        Add(buffs, Lbl("Tuş", 185, 12));
        Add(buffs, Lbl("Tekrar (saniye)", 335, 12));
        Add(buffs, Lbl("Kullanım sonrası (ms)", 485, 12));
        string[] names = ["Wolf", "DEF 200", "DEF 400", "DEF 800", "TS", "Magic Hammer", "Özel 1", "Özel 2"];
        for (int i = 0; i < 8; i++)
        {
            int y = 36 + i * 32;
            _buffEnabled[i].Text = names[i]; _buffEnabled[i].Location = new Point(12, y + 2);
            buffs.Controls.Add(_buffEnabled[i]);
            Add(buffs, KeyBox("BuffKey" + i, 185, y));
            Add(buffs, Num("BuffSeconds" + i, 335, y, 1, 86400));
            Add(buffs, Num("BuffCast" + i, 485, y, 20, 10000));
        }
        Add(buffs, Lbl("Atak tetiğiyle başlar. Süre dolunca combo arasına eklenir. Tek combo turu buff basmaz.", 12, 303));
        Add(buffs, Lbl("Süreleri sunucuna göre ayarla. Buff ikonunu/başarıyı okumaz; Magic Hammer eşya gerektirebilir.", 12, 327));

        _monsterEnabled.Location = new Point(12, 12); monster.Controls.Add(_monsterEnabled);
        Add(monster, Lbl("Her satıra tam mob adı; kısmi eşleşme yapılmaz.", 12, 42));
        _monsterNames.Location = new Point(12, 64); UiTheme.StyleField(_monsterNames); monster.Controls.Add(_monsterNames);
        var crop = UiTheme.NavButton("3 sn sonra hedef adını kırp");
        crop.Location = new Point(335, 64); crop.Width = 310;
        crop.Click += async (_, _) => await RunFarmActionAsync("Hedef kırpma", PickMonsterAsync);
        monster.Controls.Add(crop);
        _monsterRegion.Location = new Point(335, 101); monster.Controls.Add(_monsterRegion);
        var read = UiTheme.NavButton("3 sn sonra yalnız OCR oku");
        read.Location = new Point(335, 135); read.Width = 310;
        read.Click += async (_, _) => await RunFarmActionAsync("Hedef okuma", ReadMonsterPreviewAsync);
        monster.Controls.Add(read);
        _monsterResult.Location = new Point(335, 175); monster.Controls.Add(_monsterResult);
        Add(monster, Lbl("Z sonrası bekleme (ms)", 12, 242));
        Add(monster, Num("MonsterSettle", 12, 265, 100, 3000));
        Add(monster, Lbl("Sadece hedef adı kırpılmalı; HP/rakam/başka metin dahil edilmemeli. Her saldırı tuşu öncesi OCR yapılır.", 12, 305));
        Add(monster, Lbl("OCR combo hızını düşürebilir. Boyut/UI ölçeği değişirse yeniden kırp.", 12, 329));
        Add(monster, Lbl("Filtre, oyunda başlamış otomatik saldırıyı durdurmaz; yanlış hedefe hiç vurulmayacağı garanti değildir.", 12, 353));

        Add(market, Lbl("Her satır bir mesaj. Liste sırayla döner; en fazla 160 karakter/mesaj.", 12, 12));
        _marketMessages.Location = new Point(12, 36); UiTheme.StyleField(_marketMessages); market.Controls.Add(_marketMessages);
        Add(market, Lbl("Aralık (saniye)", 12, 191)); Add(market, Num("MarketSeconds", 12, 215, 10, 3600));
        Add(market, Lbl("Toplam gönderim", 165, 191)); Add(market, Num("MarketRepeat", 165, 215, 1, 1000));
        _chatClosed.Location = new Point(12, 253); market.Controls.Add(_chatClosed);
        var send = UiTheme.NavButton("3 sn sonra pazarı başlat");
        send.Location = new Point(335, 208); send.Width = 310;
        send.Click += async (_, _) =>
        {
            if (!_chatClosed.Checked) { _status.Text = "Önce sohbetin kapalı olduğunu onaylayın."; return; }
            await RunFarmActionAsync("Pazar", RunMarketAsync);
            _chatClosed.Checked = false;
        };
        market.Controls.Add(send);
        Add(market, Lbl("Combo/Minor ile aynı anda çalışmaz. F9/Durdur iptal eder; odak kaybında otomatik devam etmez.", 12, 293));
        Add(market, Lbl("Kesilirse oyun sohbetini kontrol edip ESC ile kapat. Mesajın sunucuya ulaştığı doğrulanmaz.", 12, 319));
    }

    private void ApplyFarmToUi()
    {
        var farm = _settings.Farm ?? new FarmSettings();
        for (int i = 0; i < 8; i++)
        {
            var buff = farm.Buffs?.ElementAtOrDefault(i) ?? new BuffSetting { Name = _buffEnabled[i].Text };
            _buffEnabled[i].Checked = buff.Enabled;
            SetKey("BuffKey" + i, buff.Key);
            SetNum("BuffSeconds" + i, buff.IntervalSeconds);
            SetNum("BuffCast" + i, buff.CastWaitMs);
        }
        _monsterEnabled.Checked = farm.Monster.Enabled;
        _monsterNames.Lines = farm.Monster.Names?.ToArray() ?? [];
        _monsterRegion.Text = "Oyun içi bölge: " + farm.Monster.Region;
        SetNum("MonsterSettle", farm.Monster.SettleMs);
        _marketMessages.Lines = farm.Market.Messages?.ToArray() ?? [];
        SetNum("MarketSeconds", farm.Market.IntervalSeconds);
        SetNum("MarketRepeat", farm.Market.RepeatCount);
        _chatClosed.Checked = false;
        _monsterResult.Text = "";
    }

    private FarmSettings ReadFarmUi()
    {
        var farm = Settings.Snapshot(_settings.Farm ?? new FarmSettings());
        farm.Buffs = Enumerable.Range(0, 8).Select(i => new BuffSetting
        {
            Enabled = _buffEnabled[i].Checked, Name = _buffEnabled[i].Text, Key = KeyVal("BuffKey" + i),
            IntervalSeconds = Fallback("BuffSeconds" + i, 60), CastWaitMs = Fallback("BuffCast" + i, 1000)
        }).ToList();
        farm.Monster.Enabled = _monsterEnabled.Checked;
        farm.Monster.Names = _monsterNames.Lines.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        farm.Monster.SettleMs = Fallback("MonsterSettle", 250);
        farm.Market.Messages = _marketMessages.Lines.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        farm.Market.IntervalSeconds = Fallback("MarketSeconds", 30);
        farm.Market.RepeatCount = Fallback("MarketRepeat", 10);
        return farm;
    }

    private MacroEngine CreateEngine() => new(_settings, _input,
        _settings.Farm.Monster.Enabled ? new WindowMonsterReader(_settings.TargetProcess, _settings.Farm.Monster) : null);

    private async Task RunFarmActionAsync(string name, Func<CancellationToken, Task> action)
    {
        if (_closing || !_probeTask.IsCompleted || (_engine != null && !_engine.Completion.IsCompleted)) return;
        try
        {
            _settings = ReadUiDraft();
            _input.Configure(_settings); _input.InstallHooks(); _hotkeyManager.RegisterHotkeys(_settings);
            _input.SetArmed(false);
            _engine = null;
            _farmAction = name;
            _probeStop?.Dispose(); _probeStop = new CancellationTokenSource();
            _status.Text = name + ": 3 saniye içinde oyuna geç. Durdur/F9 iptal eder.";
            _probeTask = action(_probeStop.Token);
            RefreshStatus();
            await _probeTask;
        }
        catch (OperationCanceledException) { _status.Text = name + " iptal edildi. Pazar yarıda kaldıysa oyun sohbetini ESC ile kapat."; }
        catch (Exception ex) { _status.Text = name + ": " + ex.Message + (name == "Pazar" ? " Sohbeti kontrol et, gerekiyorsa ESC ile kapat." : ""); }
        finally { _farmAction = null; _marketRunner = null; _input.SetArmed(false); RefreshStatus(); }
    }

    private async Task FarmCountdownAsync(CancellationToken token)
    {
        for (int i = 0; i < 60; i++)
        {
            token.ThrowIfCancellationRequested();
            if (_input.IsHeld(_settings.Hotkeys.EmergencyStop)) throw new OperationCanceledException();
            await Task.Delay(50, token);
        }
        token.ThrowIfCancellationRequested();
    }

    private async Task PickMonsterAsync(CancellationToken token)
    {
        await FarmCountdownAsync(token);
        IntPtr window = GameWindow.RequireForeground(_settings.TargetProcess);
        Rectangle bounds = GameWindow.ClientBounds(window);
        using var picker = new OcrOverlay();
        // Modal seçimde de F9, acil tuş ve kapanış iptali işlensin.
        using var stopTimer = new System.Windows.Forms.Timer { Interval = 50 };
        stopTimer.Tick += (_, _) =>
        {
            if (!token.IsCancellationRequested && !_input.IsHeld(_settings.Hotkeys.EmergencyStop)) return;
            picker.DialogResult = DialogResult.Cancel;
            picker.Close();
        };
        stopTimer.Start();
        if (picker.ShowDialog() != DialogResult.OK) return;
        token.ThrowIfCancellationRequested();
        Rectangle r = picker.ScreenRect;
        if (GameWindow.ClientBounds(window) != bounds || !bounds.Contains(r))
            throw new InvalidOperationException("Kırpılan alan oyunun içinde olmalı; seçim sırasında pencereyi taşımayın.");
        _settings.Farm.Monster.Region = new UiRegion { X = r.X - bounds.X, Y = r.Y - bounds.Y, W = r.Width, H = r.Height };
        _settings.Farm.Monster.ClientWidth = bounds.Width; _settings.Farm.Monster.ClientHeight = bounds.Height;
        _monsterRegion.Text = "Oyun içi bölge: " + _settings.Farm.Monster.Region;
        _status.Text = "Hedef adı kırpıldı. Önce yalnız OCR oku ile doğrula.";
    }

    private async Task ReadMonsterPreviewAsync(CancellationToken token)
    {
        await FarmCountdownAsync(token);
        var filter = Settings.Snapshot(_settings.Farm.Monster);
        filter.Enabled = true; filter.Validate();
        var reader = new WindowMonsterReader(_settings.TargetProcess, filter);
        string text = await reader.ReadAsync(token).WaitAsync(TimeSpan.FromSeconds(2), token);
        _monsterResult.Text = "OCR: " + text + "\n" + (filter.Allows(text) ? "İzin verilen isim" : "Eşleşme yok — saldırı engellenir");
        _status.Text = "Hedef OCR okundu; hiçbir tuş gönderilmedi.";
    }

    private async Task RunMarketAsync(CancellationToken token)
    {
        _settings.Farm.Market.Validate();
        await FarmCountdownAsync(token);
        if (InputDiagnostics.Active) throw new InvalidOperationException("Pazar için önce tanılama kaydını bitirin.");
        _marketRunner = new MarketRunner(_settings.Farm.Market, _input, new WindowsChatEncoder(_settings.TargetProcess),
            _settings.TargetProcess, _settings.Hotkeys.EmergencyStop);
        await _marketRunner.RunAsync(token);
        _status.Text = $"Pazar tamamlandı: {_marketRunner.SentCount} gönderim dizisi. Sunucu teslimi doğrulanmadı.";
    }
}
