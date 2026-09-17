using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using KOPunisher;

internal static class Program
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;
    private static object? Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private)!.Invoke(target, args);
    private static IEnumerable<Control> Desc(Control c)
    {
        foreach (Control child in c.Controls)
        {
            yield return child;
            foreach (var nested in Desc(child)) yield return nested;
        }
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string workspace = Path.Combine(Path.GetTempPath(), "ko-ui-" + Guid.NewGuid());
        Directory.CreateDirectory(workspace);
        try
        {
            using (var form = new MainForm(Path.Combine(workspace, "settings.json")))
            {
                try
                {
                    string screenshotDir = Path.Combine(Path.GetTempPath(), "ko-punisher-layout-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(screenshotDir);
                    void Shot(string name)
                    {
                        using var bitmap = new Bitmap(form.Width, form.Height);
                        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                        bitmap.Save(Path.Combine(screenshotDir, name + ".png"));
                    }
                    // Diskteki kullanıcı ayarlarını değiştirmeden gerçek UI bağlamasını sına.
                    var settings = new Settings { MinorPedalKey = "2", RunMode = RunMode.Toggle };
                    settings.Skills["Thrust"] = "F7";
                    settings.EnsureDefaultProfiles();
                    typeof(MainForm).GetField("_settings", Private)!.SetValue(form, settings);
                    Call(form, "ApplyToUi");
                    foreach (string page in new[] { "assassin", "farm", "settings", "upgrade", "assassin" })
                    {
                        Call(form, "ShowPage", page);
                        var current = (Settings)Call(form, "ReadUi")!;
                        Check(current.MinorHoldMs == 25 && current.MinorRepeatMs == 100, "Sekme zamanlamayı sıfırladı.");
                        Check(current.MinorPedalKey == "2" && current.Hotkeys.EmergencyStop == "F12", "Sekme tuşları sıfırladı.");
                        Check(current.RunMode == RunMode.Toggle, "Sekme çalışma modunu sıfırladı.");
                        Check(current.Skills["Thrust"] == "F7" && current.FillerOrder.Contains("Thrust"), "Bar görünmeyen F tuşunu sildi.");
                    }
                    Console.WriteLine("PASS İlk açılış/sekme geçişlerinde ayarlar ve geçerli ReadUi");
                    var box = Field<Dictionary<string, TextBox>>(form, "_keys")["ComboKey1"];
                    var onKeyDown = typeof(Control).GetMethod("OnKeyDown", Private)!;
                    for (int i = 0; i < 10; i++)
                    {
                        onKeyDown.Invoke(box, new object[] { new KeyEventArgs((Keys)((int)Keys.D0 + i)) });
                        Check(box.Text == i.ToString(), "Üst sıra rakam eşlemesi yanlış.");
                        onKeyDown.Invoke(box, new object[] { new KeyEventArgs((Keys)((int)Keys.NumPad0 + i)) });
                        Check(box.Text == "NUMPAD" + i, "Numpad eşlemesi yanlış.");
                    }
                    Console.WriteLine("PASS Gerçek TextBox KeyDown: D0–D9 ve NUMPAD0–9");
                    var keys = Field<Dictionary<string, TextBox>>(form, "_keys");
                    var presets = Field<ComboBox>(form, "_preset");
                    foreach (var (page, preset, type) in new[]
                    {
                        ("archery", "70-72", ClassType.Archer),
                        ("mage", "staff-r", ClassType.Mage),
                        ("priest", "bp-rr", ClassType.BattlePriest),
                        ("assassin", "assassin-rr", ClassType.Assassin)
                    })
                    {
                        Call(form, "ShowPage", page);
                        Check(presets.Items.Contains(preset), "Job preset eksik: " + preset);
                        presets.SelectedItem = preset;
                        keys["ComboKey1"].Text = "F6";
                        keys["ComboKey2"].Text = page == "archery" ? "F8" : "";
                        keys["ComboKey3"].Text = "";
                        var current = (Settings)Call(form, "ReadUi")!;
                        Check(current.ClassType == type && current.ComboPreset == preset, "Job/preset bağlaması");
                        Check(current.ComboKey1 == "F6" && (page != "assassin" || current.Skills["Spike"] == "3"), "Job tuşları ezdi");
                        current.ActiveProfile = current.Profiles[0].Name;
                        current.SaveCurrentToProfile();
                        var restored = new Settings { Profiles = current.Profiles };
                        restored.ApplyProfile(current.ActiveProfile);
                        Check(restored.ComboPreset == preset && restored.ComboKey1 == "F6", "Preset profil roundtrip");
                        Check(Field<Label>(form, "_comboGuide").Text.Length > 30, "Combo açıklaması eksik");
                    }
                    Console.WriteLine("PASS Dört job preset/özel tuş/profil bağlaması; okçu 3/5 atamalarını ezmez");
                    Call(form, "ShowPage", "warrior");
                    Call(form, "ShowJobTab", "yan");
                    var supportPane = Field<Panel>(form, "_minorPane");
                    Check(Desc(supportPane).OfType<Label>().Any(l => l.Text.Contains("Yan skill kataloğu")) &&
                        !Desc(supportPane).OfType<Label>().Any(l => l.Text == "Minor"), "Warrior Yan Skiller generic Minor controls leaked");
                    Call(form, "ShowPage", "assassin");
                    Call(form, "ShowJobTab", "yan");
                    Check(Desc(supportPane).Contains(keys["MinorTrigger"]) &&
                        Desc(supportPane).OfType<Label>().Any(l => l.Text.Contains("Minor")), "Assassin Minor controls missing");
                    Call(form, "ShowPage", "priest");
                    Call(form, "ShowJobTab", "debuff");
                    Check(Desc(supportPane).OfType<Label>().Any(l => l.Text.Contains("Priest debuff")) &&
                        Desc(supportPane).OfType<Label>().Any(l => l.Text.Contains("Malice") || l.Text.Contains("Parasite")),
                        "Priest Debuff category content missing");
                    Check(!Desc(supportPane).OfType<Label>().Any(l => l.Text.Contains("Healing") ||
                        l.Text.Contains("Restore") || l.Text is "Massive Binder" or "Massiveness" or "Collapse"),
                        "Priest Debuff list leaked healing, buffs or attack skills through substring matching");
                    Check(Desc(supportPane).OfType<Label>().Any(l => l.Text == "Massive"), "Massive debuff missing");
                    Call(form, "ShowJobTab", "yan");
                    Check(!Desc(supportPane).OfType<Label>().Any(l => l.Text is "Malice" or "Parasite" or "Massive"),
                        "Priest Buff-Heal-Cure duplicated Debuff skills");
                    Check(Desc(supportPane).OfType<Label>().Any(l => l.Text == "Cure Curse"), "Priest Cure skill missing");
                    Call(form, "ShowPage", "archery");
                    Call(form, "ShowJobTab", "diger");
                    form.Show(); Application.DoEvents();
                    var potionPane = Field<Panel>(form, "_digerPane");
                    var regionButtons = Desc(potionPane).OfType<Button>().Where(b => b.Text.EndsWith("bölge çiz")).ToArray();
                    Check(regionButtons.Length == 2 && regionButtons.All(b => b.Right <= b.Parent!.ClientSize.Width),
                        "HP/MP region buttons clipped by persistent preview");
                    Type previewType = typeof(MainForm).Assembly.GetType("KOPunisher.SkillLayoutPreviewControl")!;
                    var bounds = previewType.GetMethod("SlotBounds", BindingFlags.Static | BindingFlags.NonPublic)!;
                    for (int i = 0; i < 10; i++)
                    {
                        var r = (Rectangle)bounds.Invoke(null, new object[] { 220, i })!;
                        Check(r.Left >= 0 && r.Right <= 220 && r.Height >= 28, "Preview slot clipped at min width");
                    }
                    Call(form, "ShowPage", "assassin"); Call(form, "ShowJobTab", "atak"); Shot("assassin-attack");
                    Call(form, "ShowJobTab", "diger"); Shot("assassin-diger");
                    Call(form, "ShowPage", "priest"); Call(form, "ShowJobTab", "debuff"); Shot("priest-debuff");
                    Console.WriteLine("Layout screenshots: " + screenshotDir);
                    Console.WriteLine("PASS Job category panes and fixed preview geometry");
                    Call(form, "ShowPage", "archery");
                    foreach (string preset in new[] { "3-5", "5-3", "3-5-w", "3-w-5-w", "70-72-60" })
                    {
                        presets.SelectedItem = preset;
                        keys["ComboKey1"].Text = "F6";
                        keys["ComboKey2"].Text = "F7";
                        keys["ComboKey3"].Text = "F8";
                        var preview = Field<FlowLayoutPanel>(form, "_comboPreview");
                        int expected = preset.Contains('w') ? 4 : preset == "70-72-60" ? 3 : 2;
                        Check(preview.Controls.Count == expected, "Preview step count");
                        string first = preview.Controls[0].Controls.OfType<Label>().Single().Text;
                        Check(first.Contains(preset == "3-5-w" ? "F7" : "F6"), "Preview actual first key");
                        var help = Field<ToolTip>(form, "_help");
                        Check(help.GetToolTip(presets) == ComboCatalog.Description(preset), "Combo tooltip mismatch");
                        Check(presets.GetItemText(preset) == ComboCatalog.Title(preset), "Combo title mismatch");
                        Check(help.GetToolTip(keys["ComboKey1"]) == "Skill tuşu", "Key tooltip missing");
                    }
                    Check(!Field<CheckBox>(form, "_quickCombo").Checked, "Timing tests must default to configured values");
                    Check(!Field<CheckBox>(form, "_quickCombo").Bounds.IntersectsWith(Field<Button>(form, "_singleCombo").Bounds), "Test controls overlap");
                    Console.WriteLine("PASS Combo titles, short tooltips, actual-key images and quick-test controls");
                    var nums = Field<Dictionary<string, NumericUpDown>>(form, "_nums");
                    presets.SelectedItem = ComboCatalog.FiveSlideThreeSlide;
                    keys["ComboKey1"].Text = "6"; keys["ComboKey2"].Text = "5";
                    int value = 240;
                    foreach (string key in new[] { "ArcherSkillHold", "ArcherSkillAfter", "ArcherSlideHold", "ArcherSlideAfter", "ArcherCycleAfter" })
                    {
                        nums[key].Value = value++;
                        var draft = (Settings)Call(form, "ReadUiDraft", false)!;
                        Check((int)typeof(Timings).GetProperty(key)!.GetValue(draft.Timings)! == (int)nums[key].Value, "Archer timing UI binding: " + key);
                    }
                    nums["ArcherCycleAfter"].Value = 0;
                    Check(((Settings)Call(form, "ReadUiDraft", false)!).Timings.ArcherCycleAfter == 0, "Zero cycle delay lost");
                    Check(Field<Label>(form, "_archerTimingSequence").Text.Contains("5 bas") &&
                        Field<Label>(form, "_archerTimingSequence").Text.Contains("0 ms"), "Live event timeline missing");
                    var reference = Field<Panel>(form, "_digerPane").Controls.Find("ArcherReference", false).Single();
                    typeof(Control).GetMethod("OnClick", Private)!.Invoke(reference, new object[] { EventArgs.Empty });
                    var applied = (Settings)Call(form, "ReadUi")!;
                    Check(applied.RunMode == RunMode.Hold && applied.ComboPreset == ComboCatalog.FiveSlideThreeSlide &&
                        applied.ComboKey1 == "6" && applied.ComboKey2 == "5" && applied.SlideKey == "W", "Reference button bindings");
                    Check(ComboCatalog.SlideSteps(applied).SequenceEqual(new[] { ("5", 230, 230), ("W", 19, 19), ("6", 230, 230), ("W", 19, 0) }), "Reference button timings");
                    Console.WriteLine("PASS Independent archer timing UI bindings, reference button and zero-delay timeline");
                    var jobs = new[] { ("archery", "3-w-5-w", "F6", 151), ("mage", "staff-r", "F7", 252), ("priest", "bp-rr", "F10", 353) };
                    foreach (var (page, preset, key, speed) in jobs)
                    {
                        Call(form, "ShowPage", page);
                        presets.SelectedItem = preset;
                        keys["ComboKey1"].Text = key;
                        nums["ComboSpeedMs"].Value = speed;
                        Call(form, "ShowPage", "settings");
                        Call(form, "ShowPage", page);
                        var draft = (Settings)Call(form, "ReadUiDraft", false)!;
                        Check(draft.ComboPreset == preset && draft.ComboKey1 == key && draft.ComboSpeedMs == speed, "Home navigation lost job edits");
                    }
                    foreach (var (page, preset, key, speed) in jobs)
                    {
                        Call(form, "ShowPage", page);
                        var draft = (Settings)Call(form, "ReadUiDraft", false)!;
                        Check(draft.ComboPreset == preset && draft.ComboKey1 == key && draft.ComboSpeedMs == speed,
                            $"Cross-job isolation failed {page}: {draft.ComboPreset}/{draft.ComboKey1}/{draft.ComboSpeedMs}");
                    }
                    Check(Field<Button>(form, "_singleCombo").Parent != null, "Single-cycle button missing");
                    Type overlayType = typeof(MainForm).Assembly.GetType("KOPunisher.RuntimeOverlay")!;
                    using var overlay = (Form)Activator.CreateInstance(overlayType)!;
                    var cp = (CreateParams)overlayType.GetProperty("CreateParams", Private)!.GetValue(overlay)!;
                    Check((cp.ExStyle & 0x08000020) == 0x08000020 && !overlay.ShowInTaskbar, "Overlay steals focus/clicks");
                    Check((bool)overlayType.GetProperty("ShowWithoutActivation", Private)!.GetValue(overlay)!, "Overlay activates on show");
                    Console.WriteLine("PASS Job/settings roundtrip and overlay nonactivation flags");
                    Call(form, "ShowPage", "farm");
                    Field<CheckBox[]>(form, "_buffEnabled")[0].Checked = true;
                    keys["BuffKey0"].Text = "0";
                    nums["BuffSeconds0"].Value = 42; nums["BuffCast0"].Value = 300;
                    Field<CheckBox>(form, "_monsterEnabled").Checked = true;
                    Field<TextBox>(form, "_monsterNames").Lines = ["Apostle", "Deruvish"];
                    Field<Settings>(form, "_settings").Farm.Monster.Region = new UiRegion { W = 120, H = 30 };
                    Field<TextBox>(form, "_marketMessages").Lines = ["Selling", "Buying"];
                    nums["MarketSeconds"].Value = 15; nums["MarketRepeat"].Value = 3;
                    var farmDraft = (Settings)Call(form, "ReadUi")!;
                    Check(farmDraft.Farm.Buffs[0].Enabled && farmDraft.Farm.Buffs[0].Key == "0" && farmDraft.Farm.Buffs[0].IntervalSeconds == 42,
                        "Buff UI binding");
                    Check(farmDraft.Farm.Monster.Names.SequenceEqual(new[] { "Apostle", "Deruvish" }) && farmDraft.Farm.Market.RepeatCount == 3,
                        "Monster/market UI binding");
                    Call(form, "ShowPage", "archery");
                    Check(!Field<CheckBox[]>(form, "_buffEnabled")[0].Checked && !Field<CheckBox>(form, "_monsterEnabled").Checked,
                        "Farm UI leaked across jobs");
                    Call(form, "ShowPage", "priest");
                    Check(Field<CheckBox[]>(form, "_buffEnabled")[0].Checked && Field<TextBox>(form, "_marketMessages").Lines.Length == 2,
                        "Farm UI job restoration");
                    var busy = new TaskCompletionSource();
                    typeof(MainForm).GetField("_probeTask", Private)!.SetValue(form, busy.Task);
                    Call(form, "RefreshStatus");
                    Check(!Field<Button>(form, "_start").Enabled && !Field<Button>(form, "_singleCombo").Enabled &&
                        !Field<Control>(form, "_farmPage").Enabled, "Market/test does not isolate GUI actions");
                    bool invoked = false;
                    var blocked = (Task)Call(form, "RunFarmActionAsync", "Pazar", new Func<CancellationToken, Task>(_ => { invoked = true; return Task.CompletedTask; }))!;
                    blocked.GetAwaiter().GetResult();
                    Call(form, "Start");
                    Check(!invoked && typeof(MainForm).GetField("_engine", Private)!.GetValue(form) == null, "Busy market/test allowed another input lane");
                    busy.SetResult(); Call(form, "RefreshStatus");
                    Console.WriteLine("PASS Farm GUI bindings, job isolation and market/combo/test exclusion");
                    var priestSettings = new Settings { ClassType = ClassType.Priest, ComboPreset = "rr" };
                    priestSettings.EnsureDefaultProfiles();
                    typeof(MainForm).GetField("_settings", Private)!.SetValue(form, priestSettings);
                    Call(form, "ApplyToUi");
                    Call(form, "ShowPage", "priest");
                    presets.SelectedItem = "rr";
                    keys["ComboKey1"].Text = "6";
                    keys["HpPotionFallbackKey"].Text = "4";
                    presets.SelectedItem = "bp-rr";
                    keys["ComboKey1"].Text = "7";
                    keys["HpPotionFallbackKey"].Text = "5";
                    presets.SelectedItem = "rr";
                    var priestDraft = (Settings)Call(form, "ReadUiDraft", false)!;
                    Check(priestDraft.ClassType == ClassType.Priest && priestDraft.ComboKey1 == "6" &&
                        priestDraft.HealthMana.Hp.FallbackKey == "4", "Priest mode draft not restored");
                    presets.SelectedItem = "bp-rr";
                    var bpDraft = (Settings)Call(form, "ReadUiDraft", false)!;
                    Check(bpDraft.ClassType == ClassType.BattlePriest && bpDraft.ComboKey1 == "7" &&
                        bpDraft.HealthMana.Hp.FallbackKey == "5", "Battle Priest mode draft not restored");
                    Call(form, "ShowPage", "warrior");
                    Call(form, "ShowPage", "priest");
                    bpDraft = (Settings)Call(form, "ReadUiDraft", false)!;
                    Check(bpDraft.ClassType == ClassType.BattlePriest && bpDraft.ComboPreset == "bp-rr" &&
                        bpDraft.ComboKey1 == "7" && bpDraft.HealthMana.Hp.FallbackKey == "5",
                        "Navigation rebuilt preset options and silently switched away from saved BP mode");
                    Console.WriteLine("PASS Priest/BP mode drafts, potion keys and navigation restoration");

                }
                finally
                {
                    Field<System.Windows.Forms.Timer>(form, "_timer").Dispose();
                    Field<WindowsMacroInput>(form, "_input").Dispose();
                    Field<HotkeyManager>(form, "_hotkeyManager").Dispose();
                }
            }
            // Gerçek Windows VK → scan-code eşlemesini, input göndermeden doğrula.
            var factory = typeof(InputSender).GetMethod("CreateKeyboardInput", BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (ushort vk in new ushort[] { 0x33, 0x57, 0x52, 0x70, 0x61, 0x11 })
            {
                foreach (ushort up in new ushort[] { 0, 2 })
                {
                    object input = factory.Invoke(null, new object[] { up, vk })!;
                    object union = input.GetType().GetField("u")!.GetValue(input)!;
                    object key = union.GetType().GetField("ki")!.GetValue(union)!;
                    Check((ushort)key.GetType().GetField("wVk")!.GetValue(key)! == 0, "Scan-code modunda VK sıfır olmalı.");
                    Check((ushort)key.GetType().GetField("wScan")!.GetValue(key)! != 0, "Windows tarama kodu eşlemesi boş.");
                    Check((uint)key.GetType().GetField("dwFlags")!.GetValue(key)! == (uint)(8 | up), "Scan-code basma/bırakma bayrakları yanlış.");
                }
            }
            Console.WriteLine("PASS Windows VK → scan-code eşlemesi ve basma/bırakma");
            // Native yetki sorgusu yalnızca metadata okur; erişilemeyen PID bilinmiyor kalmalı.
            var foreground = typeof(InputSender).Assembly.GetType("KOPunisher.ForegroundDiagnostics")!;
            var integrity = foreground.GetMethod("GetIntegrity", BindingFlags.Static | BindingFlags.NonPublic)!;
            object own = integrity.Invoke(null, new object[] { (uint)Environment.ProcessId })!;
            object denied = integrity.Invoke(null, new object[] { 0u })!;
            Check(own.GetType().GetProperty("Rid")!.GetValue(own) is int, "Kendi bütünlük seviyesi okunamadı.");
            Check(denied.GetType().GetProperty("Rid")!.GetValue(denied) == null, "Okunamayan yetki tahmin edilmemeli.");
            Console.WriteLine("PASS Windows token integrity metadata ve okunamayan PID");
            if (args.Contains("--sendinput"))
                foreach (var method in Enum.GetValues<InputSender.InputMethod>()) RunInputSmoke(method);
            else Console.WriteLine("Gerçek tuş gönderimi yapılmadı. Yerel test için --sendinput ekleyin.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally { Directory.Delete(workspace, recursive: true); }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private sealed class TextBoxInput(WindowsMacroInput input, IntPtr window) : IMacroInput
    {
        public bool IsHeld(string key) => input.IsHeld(key);
        public bool IsTargetForeground(string processName) => GetForegroundWindow() == window;
        public void KeyDown(string key) => input.KeyDown(key);
        public void KeyUp(string key) => input.KeyUp(key);
        public void ReleaseAll() => input.ReleaseAll();
    }

    private static void RunInputSmoke(InputSender.InputMethod method)
    {
        InputSender.Method = method;
        using var target = new Form { Text = "KO-Punisher input testi — yalnızca bu pencereye 3wr gönderilir", Width = 700 };
        using var text = new TextBox { Dock = DockStyle.Fill, Multiline = true };
        target.Controls.Add(text);
        using var hooks = new WindowsMacroInput();
        hooks.Configure(new Settings { ComboTrigger = "3" });
        string logDirectory = Path.Combine(Path.GetTempPath(), "ko-windows-smoke-" + Guid.NewGuid());
        string logPath = InputDiagnostics.Start(logDirectory);
        Exception? failure = null;
        bool completed = false;
        target.Shown += async (_, _) =>
        {
            try
            {
                hooks.InstallHooks();
                target.Activate();
                text.Focus();
                await Task.Delay(2000);
                foreach (string key in new[] { "3", "W", "R" })
                {
                    Check(GetForegroundWindow() == target.Handle && text.Focused, "Test odağı kayboldu; gönderim iptal.");
                    try
                    {
                        InputSender.KeyDown(key);
                        await Task.Delay(50);
                    }
                    finally { InputSender.KeyUp(key); }
                    await Task.Delay(50);
                }
                Check(text.Text.Equals("3wr", StringComparison.OrdinalIgnoreCase), "Gerçek SendInput sonucu beklenenden farklı: " + text.Text);
                Check(!hooks.IsHeld("3") && !hooks.IsHeld("W") && !hooks.IsHeld("R"), "Sentetik input fiziksel tetik olarak okunmamalı.");
                await InputDiagnostics.StopAsync();
                var records = File.ReadLines(logPath).Select(line => System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(line)).ToArray();
                var sent = records.Where(e => e.GetProperty("kind").GetString() == "send_result").ToArray();
                Check(sent.Length == 6 && sent.All(e =>
                    e.GetProperty("data").GetProperty("method").GetString() == method.ToString() &&
                    (method == InputSender.InputMethod.SendInputScanCode
                        ? e.GetProperty("data").GetProperty("accepted").GetUInt32() == 1
                        : e.GetProperty("data").GetProperty("accepted").ValueKind == System.Text.Json.JsonValueKind.Null)),
                    "Altı down/up doğru yöntem ve API semantiğiyle loglanmalı.");
                Check(records.Count(e => e.GetProperty("kind").GetString() == "own_injected_hook") == 6, "Kendi sentetik olayları hook'ta gözlenmeli.");
                Check(!records.Any(e => e.GetProperty("kind").GetString() == "physical_trigger"), "Sentetik olay fiziksel tetik kaydı üretmemeli.");
                text.Clear();
                Type encoderType = typeof(InputSender).Assembly.GetType("KOPunisher.WindowsChatEncoder")!;
                string process = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                var encoder = (IChatEncoder)Activator.CreateInstance(encoderType, process)!;
                string sample = "Satilik 123!";
                if (InputLanguage.CurrentInputLanguage.Culture.TwoLetterISOLanguageName == "tr") sample += " ığüşöç İĞÜŞÖÇ";
                var market = new MarketRunner(new MarketSettings { Messages = [sample], RepeatCount = 1 },
                    new TextBoxInput(hooks, target.Handle), encoder, process, "F12");
                await market.RunAsync(CancellationToken.None);
                Check(text.Text.Trim('\r', '\n') == sample && market.SentCount == 1, "Pazar native text/layout roundtrip");
                Console.WriteLine($"PASS Pazar {method} → TextBox: karakter düzeni, Shift ve Enter sırası");
                completed = true;
                Console.WriteLine($"PASS Gerçek Windows {method} → TextBox: 3wr, API logları ve sentetik/fiziksel ayrımı");
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                try { InputSender.CleanupAllKeys(); }
                finally { await InputDiagnostics.StopAsync(); target.Close(); }
            }
        };
        try
        {
            Application.Run(target);
            if (failure != null) throw failure;
            Check(completed, "Test tamamlanmadan pencere kapatıldı.");
        }
        finally
        {
            InputDiagnostics.StopAsync().GetAwaiter().GetResult();
            Directory.Delete(logDirectory, true);
        }
    }
}
