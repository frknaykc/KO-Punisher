using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm
{
    private RuntimeOverlay? _overlay;
    private readonly Button _singleCombo = UiTheme.NavButton("3 sn sonra tek tur");
    private readonly CheckBox _quickCombo = new() { Text = "Hızlı test", Checked = false, AutoSize = true, ForeColor = UiTheme.Text };
    private readonly Label _comboTestResult = new() { Location = new Point(12, 459), Size = new Size(780, 42), ForeColor = UiTheme.Gold };
    private readonly CheckBox _showOverlay = new() { Text = "Durum paneli", Checked = true, AutoSize = true, ForeColor = UiTheme.Text };

    private static ClassType? JobForPage(string page) => page switch
    {
        "assassin" => ClassType.Assassin, "archery" => ClassType.Archer,
        "warrior" => ClassType.Warrior, "priest" => ClassType.Priest,
        "mage" => ClassType.Mage, "bp" => ClassType.BattlePriest, _ => null
    };

    private void AddRuntimeControls(Control parent)
    {
        _singleCombo.Location = new Point(620, 26);
        _singleCombo.Width = 180;
        _singleCombo.Click += async (_, _) => await BeginSingleComboAsync();
        parent.Controls.Add(_singleCombo);
        _showOverlay.Location = new Point(620, 4);
        parent.Controls.Add(_showOverlay);
        _quickCombo.Location = new Point(730, 4);
        parent.Controls.Add(_quickCombo);
        parent.Controls.Add(_comboTestResult);
    }

    private async Task BeginSingleComboAsync()
    {
        if (_closing || !_probeTask.IsCompleted || _engine?.IsArmed == true || _skillLayoutEditor?.Busy == true) return;
        try
        {
            _settings = ReadUi();
            _comboTestResult.Text = ComboCatalog.IsSlide(_settings.ComboPreset) || !_quickCombo.Checked
                ? "3 saniye sonra test; CTRL gerekmez. Arayüzdeki süreler kullanılır."
                : "3 saniye sonra hızlı test: 50 ms skill, 30 ms hareket, 100 ms ara.";
            _shownError = null;
            _input.Configure(_settings);
            _input.InstallHooks();
            _hotkeyManager.RegisterHotkeys(_settings);
            _probeStop?.Dispose();
            _probeStop = new CancellationTokenSource();
            _status.Text = "3 saniye içinde oyuna geç. Tek tur; Minor döngüsü çalışmaz. Durdur/F9 iptal eder.";
            _probeTask = RunSingleComboAsync(_probeStop.Token);
            RefreshStatus();
            await _probeTask;
        }
        catch (OperationCanceledException) { _comboTestResult.Text = _status.Text = "Tek tur iptal edildi."; }
        catch (Exception ex) { _comboTestResult.Text = _status.Text = "Tek tur: " + ex.Message; }
        finally { _input.SetArmed(false); RefreshStatus(); }
    }

    private async Task RunSingleComboAsync(CancellationToken token)
    {
        // Geri sayım boyunca da acil durdurmayı kontrol et.
        for (int i = 0; i < 60; i++)
        {
            token.ThrowIfCancellationRequested();
            if (_input.IsHeld(_settings.Hotkeys.EmergencyStop)) throw new OperationCanceledException();
            await Task.Delay(50, token);
        }
        token.ThrowIfCancellationRequested();
        var test = _quickCombo.Checked ? ComboTest.QuickSettings(_settings) : Settings.Snapshot(_settings);
        _engine = new MacroEngine(test, _input,
            test.Farm.Monster.Enabled ? new WindowMonsterReader(test.TargetProcess, test.Farm.Monster) : null);
        await _engine.RunOnceAsync(token);
        string keys = string.Join(" → ", _engine.TestKeys);
        _comboTestResult.Text = $"{ComboTitle(test.ComboPreset)} · {_engine.TestKeys.Length} basış: {keys}\n" +
            (_engine.Error ?? (token.IsCancellationRequested ? "Test iptal edildi." : "Gönderim tamamlandı; oyun kabulü doğrulanmadı."));
        _status.Text = _engine.Error ?? "Tek combo turu tamamlandı.";
    }

    private async Task WaitForGameAsync(int milliseconds, CancellationToken token)
    {
        long end = Environment.TickCount64 + milliseconds;
        do
        {
            token.ThrowIfCancellationRequested();
            if (!_input.IsTargetForeground(_settings.TargetProcess) || _input.IsHeld(_settings.Hotkeys.EmergencyStop))
                throw new InvalidOperationException("Oyun odağı kayboldu veya acil durdurma istendi.");
            long left = end - Environment.TickCount64;
            if (left <= 0) return;
            await Task.Delay((int)Math.Min(left, 5), token);
        } while (true);
    }

    private void RefreshOverlay(bool armed, bool probing)
    {
        _singleCombo.Enabled = !armed && !probing && !_closing;
        _quickCombo.Enabled = _singleCombo.Enabled && !ComboCatalog.IsSlide(_preset.SelectedItem as string ?? _settings.ComboPreset);
        bool paused = armed && _engine!.FocusPaused;
        _input.SetArmed(armed && !paused);
        if (!_showOverlay.Checked || _closing || _farmAction != null || (!armed && !probing))
        {
            _overlay?.Hide();
            return;
        }
        _overlay ??= new RuntimeOverlay();
        string state = paused ? "Duraklatıldı · oyuna dön, tetiği bırak / yeniden bas"
            : _engine?.ComboActive == true || _engine?.MinorActive == true ? "Çalışıyor"
            : probing ? "Tek tur testi / geri sayım" : "Hazır · yeni tetik bekleniyor";
        _overlay.UpdateText($"{_settings.ClassType} · {ComboTitle(_settings.ComboPreset)}\n{state}\nTetik: {_settings.ComboTrigger} · Minor: {_settings.MinorTrigger} · Durdur: {_settings.Hotkeys.Stop}");
        if (!_overlay.Visible)
        {
            var area = Screen.FromControl(this).WorkingArea;
            _overlay.Location = new Point(area.Left + 12, area.Top + 12);
            _overlay.Show();
        }
    }
}

// Ayrı masaüstü penceresi: oyun belleğine/render sürecine enjekte edilmez.
internal sealed class RuntimeOverlay : Form
{
    private readonly Label _text = new() { Dock = DockStyle.Fill, Padding = new Padding(10), ForeColor = UiTheme.Text };
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 | 0x00000020 | 0x00000080; // NOACTIVATE, TRANSPARENT, TOOLWINDOW
            return cp;
        }
    }
    public RuntimeOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(500, 88);
        BackColor = UiTheme.Panel;
        Opacity = 0.88;
        Controls.Add(_text);
    }
    public void UpdateText(string text) { if (_text.Text != text) _text.Text = text; }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0084) { m.Result = new IntPtr(-1); return; } // HTTRANSPARENT
        if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; } // MA_NOACTIVATE
        base.WndProc(ref m);
    }
}
