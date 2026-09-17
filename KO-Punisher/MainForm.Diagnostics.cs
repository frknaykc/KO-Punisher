using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace KOPunisher;

public partial class MainForm
{
    private readonly Label _diagnosticInfo = new() { AutoSize = false, Location = new Point(12, 215), Size = new Size(810, 100), ForeColor = UiTheme.Text };
    private readonly TextBox _diagnosticKey = new() { Text = "3", Location = new Point(70, 116), Width = 100, MaxLength = 12 };
    private readonly NumericUpDown _diagnosticHold = new() { Minimum = 25, Maximum = 500, Value = 100, Location = new Point(280, 116), Width = 80 };
    private readonly ComboBox _diagnosticContext = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(70, 154), Width = 230 };
    private string? _diagnosticPath;
    private long _nextDiagnosticSample;
    private Task _diagnosticFinish = Task.CompletedTask;

    private Control DiagnosticsPage()
    {
        var p = new Panel { BackColor = UiTheme.Bg, AutoScroll = true };
        Add(p, Lbl("Tanılama: tetik → motor → Windows. Windows kabulü, oyunun kabulü demek değildir.", 12, 12));
        Add(p, Lbl("Normal yazı, pencere başlığı ve ekran görüntüsü kaydedilmez. Kayıt en fazla 20.000 olaydır.", 12, 36));
        var begin = UiTheme.NavButton("Kaydı başlat");
        begin.Location = new Point(12, 68);
        begin.Click += (_, _) => BeginDiagnostics();
        p.Controls.Add(begin);
        var finish = UiTheme.NavButton("Kaydı bitir");
        finish.Location = new Point(160, 68);
        finish.Click += async (_, _) => await FinishDiagnosticsAsync();
        p.Controls.Add(finish);
        var folder = UiTheme.NavButton("Log klasörünü aç");
        folder.Width = 170;
        folder.Location = new Point(308, 68);
        folder.Click += (_, _) =>
        {
            if (_diagnosticPath == null) { _diagnosticInfo.Text = "Önce Kaydı başlat."; return; }
            try { Process.Start(new ProcessStartInfo(Path.GetDirectoryName(_diagnosticPath)!) { UseShellExecute = true }); }
            catch (Exception ex) { _diagnosticInfo.Text = "Klasör açılamadı: " + ex.Message; }
        };
        p.Controls.Add(folder);
        Add(p, Lbl("Tuş", 12, 119));
        UiTheme.StyleField(_diagnosticKey);
        p.Controls.Add(_diagnosticKey);
        Add(p, Lbl("Basma (ms)", 188, 119));
        p.Controls.Add(_diagnosticHold);
        var probe = UiTheme.NavButton("3 sn sonra tek tuş");
        probe.Width = 180;
        probe.Location = new Point(390, 114);
        probe.Click += async (_, _) => await RunDiagnosticProbeAsync();
        p.Controls.Add(probe);
        Add(p, Lbl("Ortam", 12, 157));
        _diagnosticContext.Items.AddRange(new object[] { "Not Defteri", "Oyun sohbeti", "Oyun skill / kontrol" });
        _diagnosticContext.SelectedIndex = 0;
        p.Controls.Add(_diagnosticContext);
        foreach (var (text, outcome, x) in new[] { ("Tepki var", "observed", 320), ("Tepki yok", "not_observed", 468) })
        {
            var result = UiTheme.NavButton(text);
            result.Location = new Point(x, 152);
            result.Click += (_, _) =>
            {
                if (!InputDiagnostics.Active) { _diagnosticInfo.Text = "Sonuç eklemek için kayıt açık olmalı."; return; }
                InputDiagnostics.Record("user_observation", new { context = _diagnosticContext.SelectedItem?.ToString(), outcome, method = InputSender.Method.ToString() });
                _diagnosticInfo.Text = "Kullanıcı gözlemi kaydedildi: " + text + "\n" + _diagnosticPath;
            };
            p.Controls.Add(result);
        }
        Add(p, Lbl("Tek tuş testi için önce makroyu Durdur. Geri sayımda hedef pencereye geç. Acil durdurma / Durdur iptal eder.", 12, 192));
        p.Controls.Add(_diagnosticInfo);
        Add(p, Lbl("1. Kaydı başlat. Başlat'a bas, oyuna geç; tetik tuşuna birkaç kez basıp bırak.", 12, 330));
        Add(p, Lbl("2. Durdur. Ortamı seç; tek tuş testini Not Defteri, oyun sohbeti ve skill için ayrı dene.", 12, 358));
        Add(p, Lbl("3. Her denemeden sonra Tepki var / Tepki yok seç. Kaydı bitir, .jsonl dosyasını gönder.", 12, 386));
        Add(p, Lbl("Gönderim yöntemi", 12, 430));
        var method = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(170, 426), Width = 300 };
        method.Items.AddRange(new object[] { "keybd_event (alternatif)", "SendInput (scan-code / önceki)" });
        method.SelectedIndex = InputSender.Method == InputSender.InputMethod.LegacyKeybdEvent ? 0 : 1;
        method.SelectedIndexChanged += (_, _) =>
        {
            var selected = method.SelectedIndex == 0 ? InputSender.InputMethod.LegacyKeybdEvent : InputSender.InputMethod.SendInputScanCode;
            if (selected == InputSender.Method) return;
            if (!_probeTask.IsCompleted || (_engine != null && !_engine.Completion.IsCompleted))
            {
                _diagnosticInfo.Text = "Yöntem değiştirmeden önce makroyu / testi Durdur.";
                method.SelectedIndex = InputSender.Method == InputSender.InputMethod.LegacyKeybdEvent ? 0 : 1;
                return;
            }
            try { InputSender.Method = selected; }
            catch (InvalidOperationException ex)
            {
                _diagnosticInfo.Text = ex.Message;
                method.SelectedIndex = InputSender.Method == InputSender.InputMethod.LegacyKeybdEvent ? 0 : 1;
                return;
            }
            InputDiagnostics.Record("input_method_changed", new { method = selected.ToString() });
            _diagnosticInfo.Text = "Yöntem tüm makro ve tek tuş gönderimlerine uygulanır: " + method.Text;
        };
        p.Controls.Add(method);
        Add(p, Lbl("keybd_event kabul sonucu döndürmez; logdaki accepted=null hata değil, bilinmiyor demektir.", 12, 464));
        return p;
    }

    private void BeginDiagnostics()
    {
        if (_closing || !_diagnosticFinish.IsCompleted || InputDiagnostics.CurrentPath != null) return;
        if (!_probeTask.IsCompleted || (_engine != null && !_engine.Completion.IsCompleted))
        { _diagnosticInfo.Text = "Önce makroyu / testi Durdur."; return; }
        try
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "logs");
            try { _diagnosticPath = InputDiagnostics.Start(directory); }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _diagnosticPath = InputDiagnostics.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KO-Punisher", "logs"));
            }
            _settings = ReadUi();
            _input.Configure(_settings);
            _input.InstallHooks();
            _hotkeyManager.RegisterHotkeys(_settings);
            InputDiagnostics.Record("diagnostic_setup", new { _settings.ComboTrigger, _settings.MinorTrigger, emergency = _settings.Hotkeys.EmergencyStop, hooksInstalled = _input.HooksInstalled });
            _nextDiagnosticSample = 0;
            SampleDiagnostics();
            _diagnosticInfo.Text = "Kayıt açık. Tetik testi için üstteki Başlat'a bas.\n" + _diagnosticPath;
        }
        catch (Exception ex)
        {
            InputDiagnostics.Record("setup_error", new { type = ex.GetType().Name, error = ex.Message });
            _diagnosticInfo.Text = "Tanılama hazırlık hatası: " + ex.Message + "\n" + _diagnosticPath;
        }
    }

    private Task FinishDiagnosticsAsync()
    {
        if (!_diagnosticFinish.IsCompleted) return _diagnosticFinish;
        return _diagnosticFinish = FinishDiagnosticsCoreAsync();
    }

    private async Task FinishDiagnosticsCoreAsync()
    {
        try
        {
            try { await StopAsync(); }
            finally { await InputDiagnostics.StopAsync(); }
            _diagnosticInfo.Text = _diagnosticPath == null ? "Henüz kayıt oluşturulmadı." : "Kayıt tamamlandı. Bu dosyayı gönder:\n" + _diagnosticPath;
        }
        catch (Exception ex) { _diagnosticInfo.Text = "Kaydı bitirme hatası: " + ex.Message; }
    }

    private void SampleDiagnostics()
    {
        if (InputDiagnostics.CurrentPath == null) return;
        if (!InputDiagnostics.Active)
        {
            _diagnosticInfo.Text = InputDiagnostics.LastError is string error
                ? "Log yazma hatası: " + error
                : "20.000 olay sınırına ulaşıldı. Kaydı bitir ve dosyayı gönder.\n" + _diagnosticPath;
            return;
        }
        if (Environment.TickCount64 < _nextDiagnosticSample) return;
        _nextDiagnosticSample = Environment.TickCount64 + 1000;
        InputDiagnostics.Record("heartbeat", new
        {
            foreground = ForegroundDiagnostics.Describe(ForegroundDiagnostics.Current()),
            hookHandlesPresent = _input.HooksInstalled, // Handle bulunması hook'un hâlâ olay aldığını kanıtlamaz.
            armed = _engine?.IsArmed == true, comboActive = _engine?.ComboActive == true,
            minorActive = _engine?.MinorActive == true, error = _engine?.Error,
            comboHeld = _input.IsHeld(_settings.ComboTrigger), minorHeld = _input.IsHeld(_settings.MinorTrigger)
        });
    }

    private async Task RunDiagnosticProbeAsync()
    {
        if (_closing || !_diagnosticFinish.IsCompleted || !_probeTask.IsCompleted || (_engine != null && !_engine.Completion.IsCompleted)) return;
        if (!InputDiagnostics.Active) { _diagnosticInfo.Text = "Önce Kaydı başlat."; return; }
        string key = _diagnosticKey.Text.Trim().ToUpperInvariant();
        if (!InputSender.TryGetVkCode(key, out ushort vk) || vk is 0x05 or 0x06)
        { _diagnosticInfo.Text = "Geçerli bir klavye tuşu yaz: 3, W, F1, NUMPAD3 gibi."; return; }
        if (new[] { _settings.Hotkeys.Start, _settings.Hotkeys.Stop, _settings.Hotkeys.EmergencyStop }
            .Contains(key, StringComparer.OrdinalIgnoreCase))
        { _diagnosticInfo.Text = "Başlat / Durdur / acil tuşunu çıkış testi için kullanma; başka bir tuş seç."; return; }
        int hold = (int)_diagnosticHold.Value;
        string context = _diagnosticContext.SelectedItem?.ToString() ?? "unknown";
        using var stop = new CancellationTokenSource();
        _probeStop = stop;
        try
        {
            _probeTask = DiagnosticProbeCoreAsync(key, hold, context, stop.Token);
            await _probeTask;
            _diagnosticInfo.Text = "Windows gönderim çağrıları tamamlandı. Oyundaki sonucu Tepki var / yok ile işaretle.\n" + _diagnosticPath;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            InputDiagnostics.Record("probe_cancelled");
            _diagnosticInfo.Text = "Tek tuş testi iptal edildi.";
        }
        catch (Exception ex)
        {
            InputDiagnostics.Record("probe_error", new { error = ex.Message, type = ex.GetType().Name });
            _diagnosticInfo.Text = "Tek tuş testi hatası: " + ex.Message;
        }
        finally
        {
            _probeStop = null;
            try { InputSender.CleanupAllKeys(); }
            catch (Exception ex)
            {
                InputDiagnostics.Record("cleanup_error", new { error = ex.Message });
                _diagnosticInfo.Text = "Tuş bırakma hatası: " + ex.Message;
            }
        }
    }

    private async Task DiagnosticProbeCoreAsync(string key, int hold, string context, CancellationToken token)
    {
        InputDiagnostics.Record("probe_scheduled", new { key, holdMs = hold, context, delayMs = 3000, method = InputSender.Method.ToString() });
        await InputProbe.RunAsync(_input, key, hold, 3,
            i => _diagnosticInfo.Text = $"{i} saniye — hedef pencereye geç.",
            () =>
            {
                var target = ForegroundDiagnostics.Current();
                if (target.Pid == 0 || target.Pid == (uint)Environment.ProcessId)
                    throw new InvalidOperationException("Hedef pencereye geçilmedi; tuş gönderilmedi.");
                if (_input.IsHeld(_settings.Hotkeys.EmergencyStop)) throw new InvalidOperationException("Acil durdurma tuşu basılı; tuş gönderilmedi.");
                InputDiagnostics.Record("probe_begin", new { key, holdMs = hold, context, foreground = ForegroundDiagnostics.Describe(target) });
            }, token);
        InputDiagnostics.Record("probe_complete", new { key, context, gameAcceptance = "unknown" });
    }
}
