using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using KOPunisher;

if (args.Contains("--tooltip")) { Console.WriteLine($"{InventoryTooltipTests.Run()} tooltip checks passed."); return; }
if (args.Contains("--dispatch")) { Console.WriteLine($"{await SkillDispatcherTests.RunAsync()} skill dispatcher test groups passed."); return; }
if (args.Contains("--calibration")) { Console.WriteLine($"{await SkillCalibrationTests.RunAsync() + SkillIconMatcherTests.Run()} calibration checks passed."); return; }
if (args.Contains("--layout")) { Console.WriteLine($"{SkillLayoutTests.Run()} skill layout test groups passed."); return; }
if (args.Contains("--farm")) { Console.WriteLine($"{await FarmTests.RunAsync()} farm test groups passed."); return; }

int passed = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
async Task Until(Func<bool> condition, string message)
{
    long limit = Environment.TickCount64 + 3000;
    while (!condition())
    {
        if (Environment.TickCount64 > limit) throw new Exception("Timeout: " + message);
        await Task.Delay(5);
    }
}
Settings FastSettings()
{
    var s = new Settings { MinorPedalKey = "2", MinorHoldMs = 10, MinorRepeatMs = 35, EnableJitter = false };
    s.Timings = new Timings { SkillKeyHold = 10, SkillToWDelay = 5, WKeyHold = 10,
        WToRDelay = 5, RKeyHold = 10, NextSkillDelay = 10 };
    return s;
}
void Invalid(Action<Settings> change)
{
    var s = FastSettings(); change(s);
    try { s.Validate(); } catch (ArgumentException) { return; }
    throw new Exception("Invalid settings accepted");
}

var inputType = typeof(InputSender).GetNestedType("INPUT", BindingFlags.NonPublic)!;
Check(Marshal.SizeOf(inputType) == (IntPtr.Size == 8 ? 40 : 28), "INPUT ABI size");
Check(Marshal.OffsetOf(inputType, "u").ToInt32() == (IntPtr.Size == 8 ? 8 : 4), "INPUT union offset");
Pass("Windows INPUT ABI size / alignment");
var scanFactory = typeof(InputSender).GetMethod("CreateScanCodeInput", BindingFlags.Static | BindingFlags.NonPublic)!;
foreach (var (scan, expected) in new[] { (0x04u, 0x08u), (0x11u, 0x08u), (0x13u, 0x08u), (0x4Fu, 0x08u), (0xE04Fu, 0x09u) })
{
    foreach (ushort up in new ushort[] { 0, 2 })
    {
        object input = scanFactory.Invoke(null, new object[] { up, scan })!;
        object union = inputType.GetField("u")!.GetValue(input)!;
        object keyboard = union.GetType().GetField("ki")!.GetValue(union)!;
        object Value(string field) => keyboard.GetType().GetField(field)!.GetValue(keyboard)!;
        Check((uint)inputType.GetField("type")!.GetValue(input)! == 1, "Keyboard input type");
        Check((ushort)Value("wVk") == 0, "Scan-code mode must not send VK");
        Check((ushort)Value("wScan") == (scan & 0xFF), "Scan prefix must not be part of wScan");
        Check((uint)Value("dwFlags") == (expected | up), "Scan-code/extended/key-up flags");
    }
}
foreach (uint invalidScan in new uint[] { 0, 0xE000, 0xE11D, 0x10004 })
{
    bool rejected = false;
    try { scanFactory.Invoke(null, new object[] { (ushort)0, invalidScan }); }
    catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { rejected = true; }
    Check(rejected, "Unmapped/unsupported scan codes must be rejected");
}
Pass("Scan-code key down/up, extended/numpad distinction and invalid mappings");
var legacyFactory = typeof(InputSender).GetMethod("CreateLegacyKeyboardInput", BindingFlags.Static | BindingFlags.NonPublic);
Check(legacyFactory != null, "Legacy keybd_event mapping must exist");
foreach (var (vk, scan, extended) in new[] { (0x33, 0x04u, 0u), (0x61, 0x4Fu, 0u), (0x11, 0xE01Du, 1u) })
foreach (ushort up in new ushort[] { 0, 2 })
{
    object keyboard = legacyFactory!.Invoke(null, new object[] { up, (ushort)vk, scan })!;
    object Value(string field) => keyboard.GetType().GetField(field)!.GetValue(keyboard)!;
    Check((ushort)Value("wVk") == vk && (ushort)Value("wScan") == (scan & 0xFF), "Legacy VK/scan identity");
    Check((uint)Value("dwFlags") == (extended | up), "Legacy must omit SCANCODE and preserve extended/up");
}
Pass("Legacy keybd_event mapping preserves VK, extended and release flags");
InputSender.Method = InputSender.InputMethod.SendInputScanCode;
Check(InputSender.Method == InputSender.InputMethod.SendInputScanCode, "Method selection");
var heldForSwitch = (HashSet<ushort>)typeof(InputSender).GetField("HeldKeys", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
heldForSwitch.Add(0x33);
bool switchRejected = false;
try { InputSender.Method = InputSender.InputMethod.LegacyKeybdEvent; }
catch (InvalidOperationException) { switchRejected = true; }
finally { heldForSwitch.Clear(); }
Check(switchRejected && InputSender.Method == InputSender.InputMethod.SendInputScanCode, "Cannot change method while held");
InputSender.Method = InputSender.InputMethod.LegacyKeybdEvent;
bool invalidMethodRejected = false;
try { InputSender.Method = (InputSender.InputMethod)99; }
catch (ArgumentOutOfRangeException) { invalidMethodRejected = true; }
Check(invalidMethodRejected, "Invalid method rejected");
Pass("Input method selection protects held-key release and rejects invalid modes");
for (int i = 0; i < 10; i++)
    Check(InputSender.TryGetVkCode("NUMPAD" + i, out var vk) && vk == 0x60 + i, "Numpad VK identity");
Pass("Numpad remains distinct from top-row digits");
Settings.Load(Path.Combine(AppContext.BaseDirectory, "defaults.json")).Validate();
Pass("Packaged defaults validate");

Invalid(s => s.ComboPreset = "unknown");
Invalid(s => s.ComboPreset = "70-72");
Invalid(s => { s.ZKey = "Z"; s.ZAttackKey = s.ComboTrigger; });
Invalid(s => { s.ComboPreset = "3-5-w"; s.Skills.Clear(); s.FillerOrder.Clear(); s.ComboTrigger = "5"; });
Pass("Preset/farm implicit outputs cannot collide with triggers");

{
    var source = FastSettings();
    source.ComboPreset = "5-3"; source.ComboKey1 = "F1"; source.ComboKey2 = "F2";
    source.ClassType = ClassType.Archer; source.ComboSpeedMs = 123;
    source.AddProfile("Archer test", ClassType.Archer);
    source.Timings.SkillKeyHold = 99;
    source.Hotkeys.EmergencyStop = "F11";
    var restored = FastSettings();
    source.Profiles.Single(p => p.Name == "Archer test").ApplyToSettings(restored);
    Check(restored.ClassType == ClassType.Archer && restored.ComboPreset == "5-3" &&
        restored.ComboKey1 == "F1" && restored.ComboKey2 == "F2" && restored.ComboSpeedMs == 123, "Profile combo roundtrip");
    Check(restored.Timings.SkillKeyHold == 10 && restored.Hotkeys.EmergencyStop == "F12", "Profile snapshot aliases active state");
    restored.Timings.SkillKeyHold = 88;
    source.ApplyProfile("Archer test");
    Check(source.Timings.SkillKeyHold == 10, "Applied profile aliases active state");
    source.Validate();
    Pass("Profiles restore class, combo keys and independent timing/hotkey snapshots");
}

Invalid(s => s.ComboTrigger = "W");
Invalid(s => s.MinorTrigger = s.ComboTrigger);

var ctrlTrigger = FastSettings();
ctrlTrigger.ComboTrigger = "CTRL";
ctrlTrigger.Hotkeys.EmergencyStop = "F7";
ctrlTrigger.Validate();
Pass("CTRL trigger accepted");

Invalid(s => s.MinorPedalKey = "3");
Invalid(s => s.Skills["Spike"] = "XBUTTON1");
Invalid(s => s.Skills["Spike"] = "invalid");
Invalid(s => s.MinorRepeatMs = 5);
Invalid(s => s.Timings.NextSkillDelay = 0);
Invalid(s => s.FillerOrder.Add("Spike"));
Invalid(s => s.Timings = null!);
Invalid(s => s.JitterRange = 51);
Invalid(s => { s.ZKey = "Z"; s.ZInterval = 100; });
{
    var zOk = FastSettings(); zOk.ZKey = "Z"; zOk.ZInterval = 500; zOk.Validate();
}
Pass("Settings: key conflicts, invalid values, null sections rejected");

string directory = Path.Combine(Path.GetTempPath(), "ko-punisher-tests-" + Guid.NewGuid());
string file = Path.Combine(directory, "settings.json");
try
{
    var s = FastSettings(); s.ComboTrigger = " f6 ";
    s.Save(file);
    var loaded = Settings.Load(file);
    Check(loaded.ComboTrigger == "F6" && loaded.MinorPedalKey == "2", "Save/load normalization");
    loaded.MinorRepeatMs = 75; loaded.Save(file);
    Check(Settings.Load(file).MinorRepeatMs == 75 && Directory.GetFiles(directory).Length == 1, "Atomic overwrite");
    File.WriteAllText(file, "{\"Skills\":{\"Spike\":\"3\",\"Thrust\":\"4\",\"BloodyBeast\":\"5\",\"Stab\":\"6\",\"Cut\":\"7\",\"Shock\":\"8\",\"Jab\":\"9\"}}");
    Check(Settings.Load(file).ComboTrigger == "XBUTTON1", "Legacy defaults");
    foreach (string bad in new[] { "broken", "null", "{\"Timings\":null}" })
    {
        File.WriteAllText(file, bad);
        bool rejected = false;
        try { Settings.Load(file); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && File.ReadAllText(file) == bad, "Corrupt settings must not be silently replaced");
    }
    Pass("Settings: roundtrip, atomic overwrite, legacy defaults, corrupt file preservation");
}
finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

{
    var s = FastSettings(); var rotation = new SkillRotation(s);
    Check(rotation.Next(0) == "Spike", "Spike first"); rotation.Sent("Spike", 0);
    foreach (string name in s.FillerOrder)
    {
        Check(rotation.Next(1) == name, "Filler order: " + name);
        rotation.Sent(name, 1);
    }
    Check(rotation.Next(2) == null, "Cooling skills should not be sent");
    Check(rotation.Next(11000) == "Spike", "Spike deadline priority");
    Check(rotation.SpikeRemaining(1000) == 10000, "Remaining cooldown");
    Pass("Rotation: Spike priority, filler order, per-skill cooldown");
}

async Task WithEngine(Func<MacroEngine, FakeInput, Settings, Task> test, Action<Settings>? configure = null, bool preheld = false)
{
    var s = FastSettings(); configure?.Invoke(s);
    var io = new FakeInput();
    if (preheld) io.Held[s.ComboTrigger] = true;
    var engine = new MacroEngine(s, io);
    engine.Arm();
    try { await Task.Delay(70); await test(engine, io, s); }
    finally
    {
        await engine.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Check(io.Down.IsEmpty, "Key left held after stop");
    }
}

{
    string path = Path.Combine(Path.GetTempPath(), "ko-jobs-" + Guid.NewGuid() + ".json");
    try
    {
        var s = FastSettings();
        s.ComboTrigger = "CTRL";
        s.Timings.SkillKeyHold = 75;
        s.Save(path);
        s.SwitchJob(ClassType.Archer);
        Check(s.ComboPreset == "3-5-w" && s.ComboTrigger != "CTRL" && !s.Skills.ContainsKey("Spike"), "New archer inherited assassin");
        s.ComboKey1 = "F6"; s.ComboKey2 = "F7"; s.ComboSpeedMs = 170;
        s.SwitchJob(ClassType.Mage);
        Check(s.ComboKey1 == "" && s.ComboPreset == "staff-r", "Mage inherited archer keys");
        s.SaveJobDrafts(path); // Incomplete staff key is a draft, not a runnable setting.
        var restored = Settings.Load(path);
        restored.RestoreJobDraft();
        Check(restored.ComboTrigger == "CTRL" && restored.Timings.SkillKeyHold == 75, "Saved assassin changed");
        restored.SwitchJob(ClassType.Archer);
        Check(restored.ComboKey1 == "F6" && restored.ComboKey2 == "F7" && restored.ComboSpeedMs == 170, "Archer draft lost on restart");
        restored.SwitchJob(ClassType.Mage);
        Check(restored.ComboKey1 == "" && restored.ComboPreset == "staff-r", "Incomplete mage lost");
        restored.Timings.SkillKeyHold = 999;
        restored.SwitchJob(ClassType.Assassin);
        Check(restored.Timings.SkillKeyHold == 75, "Shared timing reference");
        restored.ActiveProfile = "Another profile";
        restored.SwitchJob(ClassType.Archer);
        Check(restored.ComboKey1 == "", "Job draft crossed named profiles");
        File.WriteAllText(path, "{broken");
        bool blocked = false;
        try { restored.SaveJobDrafts(path); } catch (InvalidOperationException) { blocked = true; }
        Check(blocked && File.ReadAllText(path) == "{broken", "Damaged source overwritten");
        Pass("Job defaults, deep snapshots, profile isolation, incomplete draft restart and corrupt preservation");
    }
    finally { File.Delete(path); }
}

{
    var s = FastSettings();
    s.ActiveProfile = "Default";
    s.ClassType = ClassType.Priest;
    s.ComboPreset = "rr";
    s.ComboKey1 = "F6";
    s.HealthMana.Hp.Enabled = true;
    s.HealthMana.Hp.FallbackKey = "8";
    s.SkillLayout = new SkillLayout { VisibleBars = 1, Assignments = [new SkillAssignment { Bar = 1, Slot = 4, SkillId = "Priest_Malice" }] };
    s.StoreJobDraft();
    s.SwitchJob(ClassType.BattlePriest);
    s.ComboPreset = "bp-rr";
    s.ComboKey1 = "F9";
    s.Timings.SkillKeyHold = 77;
    s.HealthMana.Hp.FallbackKey = "9";
    s.SkillLayout = new SkillLayout { VisibleBars = 2, Assignments = [new SkillAssignment { Bar = 2, Slot = 5, SkillId = "Priest_Helis" }] };
    s.StoreJobDraft();
    s.SwitchJob(ClassType.Priest);
    Check(s.ComboPreset == "rr" && s.ComboKey1 == "F6" && s.HealthMana.Hp.FallbackKey == "8" &&
        s.SkillLayout.Resolve("Priest_Malice") == new SkillAddress(1, 4), "Priest draft overwritten by BP");
    s.SwitchJob(ClassType.BattlePriest);
    Check(s.ComboPreset == "bp-rr" && s.ComboKey1 == "F9" && s.Timings.SkillKeyHold == 77 &&
        s.HealthMana.Hp.FallbackKey == "9" && s.SkillLayout.Resolve("Priest_Helis") == new SkillAddress(2, 5),
        "BattlePriest draft not restored");
    Pass("Priest and BattlePriest drafts preserve keys, timings, layout and pots independently");
}

foreach (var preset in new[] { "3-5", "5-3", "3-5-w", "3-w-5-w", "70-72", "70-60", "70-72-60", "assassin-rr", "bp-rr", "staff-r" })
{
    var s = FastSettings(); s.ComboPreset = preset; s.ComboKey1 = "F6"; s.ComboKey2 = "F7"; s.ComboKey3 = "F8";
    if (preset == "3-5") s.Timings.SkillToWDelay = 1000;
    var io = new FakeInput();
    var engine = new MacroEngine(s, io);
    await engine.RunOnceAsync().WaitAsync(TimeSpan.FromSeconds(3));
    string[] expected = preset switch
    {
        "3-5" or "5-3" or "70-72" or "70-60" => ["F6", "F7"],
        "70-72-60" => ["F6", "F7", "F8"],
        "3-5-w" => ["F7", "W", "F6", "W"],
        "3-w-5-w" => ["F6", "W", "F7", "W"],
        "assassin-rr" => ["3", "R", "R"],
        "bp-rr" => ["F6", "R", "R"],
        _ => ["F6", "R"]
    };
    Check(io.Events.Where(e => e.Down).Select(e => e.Key).SequenceEqual(expected), "Single combo order: " + preset);
    Check(!engine.IsArmed && io.Down.IsEmpty && engine.Error == null, "Single cleanup");
    int count = io.Events.Count; await Task.Delay(80);
    Check(io.Events.Count == count, "Single combo repeated");
    Pass(preset + " exactly one cycle, no physical trigger or Minor");
}
foreach (var preset in new[] { "3-5", "5-3", "3-5-w", "3-w-5-w", "70-72", "70-60", "70-72-60" })
{
    var source = FastSettings();
    source.ComboPreset = preset;
    source.ComboKey1 = "F6"; source.ComboKey2 = "F7"; source.ComboKey3 = "F8";
    source.ComboSpeedMs = source.Timings.SkillToWDelay = source.Timings.SkillKeyHold = 2000;
    source.EnableJitter = true;
    string before = System.Text.Json.JsonSerializer.Serialize(source);
    var test = ComboTest.QuickSettings(source);
    var io = new FakeInput();
    var engine = new MacroEngine(test, io);
    await engine.RunOnceAsync().WaitAsync(TimeSpan.FromMilliseconds(1500));
    string[] expected = preset switch
    {
        "3-5-w" => ["F7", "W", "F6", "W"],
        "3-w-5-w" => ["F6", "W", "F7", "W"],
        "70-72-60" => ["F6", "F7", "F8"],
        _ => ["F6", "F7"]
    };
    Check(engine.TestKeys.SequenceEqual(expected), "Quick test trace/order: " + preset);
    Check(io.Events.Where(e => e.Down).Select(e => e.Key).SequenceEqual(expected), "Quick actual sends");
    Check(io.Down.IsEmpty && engine.Error == null, "Quick cleanup");
    Check(System.Text.Json.JsonSerializer.Serialize(source) == before, "Quick test mutated saved timings");
    test.Farm.Monster.Enabled = true;
    Check(!source.Farm.Monster.Enabled, "Quick test shared farm state");
    Check(ComboCatalog.Description(preset).Split(' ').Length <= 2, "Long combo tooltip");
    Pass(preset + " fast complete sequence, trace and settings isolation");
}

foreach (string stop in new[] { "cancel", "focus", "emergency", "send-error", "before-focus", "before-cancel" })
{
    var s = FastSettings(); s.Timings.SkillKeyHold = 2000;
    var io = new FakeInput();
    using var cancel = new CancellationTokenSource();
    if (stop == "before-focus") io.Focused = false;
    if (stop == "before-cancel") cancel.Cancel();
    if (stop == "send-error") io.FailNext = true;
    var engine = new MacroEngine(s, io);
    Task run = engine.RunOnceAsync(cancel.Token);
    if (stop is "cancel" or "focus" or "emergency")
    {
        await Until(() => !io.Down.IsEmpty, "single hold");
        if (stop == "cancel") cancel.Cancel();
        if (stop == "focus") io.Focused = false;
        if (stop == "emergency") io.Held[s.Hotkeys.EmergencyStop] = true;
    }
    await run.WaitAsync(TimeSpan.FromSeconds(1));
    Check(engine.TestKeys.SequenceEqual(io.Events.Where(e => e.Down).Select(e => e.Key)), "Stopped trace mismatch");
    Check(io.Down.IsEmpty && !engine.IsArmed, "Single stopped with held key: " + stop);
    Check(io.Events.Count(e => e.Down) <= 1, "Single continued after " + stop);
    if (stop is "before-focus" or "before-cancel") Check(io.Events.IsEmpty, "Sent before guard");
    if (stop is "focus" or "send-error" or "before-focus") Check(engine.Error != null, "Missing error");
    Pass("Single cycle safe stop: " + stop);
}

foreach (var mode in new[] { RunMode.Hold, RunMode.Toggle })
{
    await WithEngine(async (engine, io, s) =>
    {
        io.Held[s.ComboTrigger] = true;
        io.Held[s.MinorTrigger] = true;
        await Until(() => io.Count("3") > 0 && io.Count("0") > 0, "both lanes active");
        io.Focused = false;
        await Until(() => io.Down.IsEmpty && !engine.ComboActive && !engine.MinorActive, "focus loss releases both lanes");
        int count = io.Events.Count;
        await Task.Delay(80);
        Check(io.Events.Count == count, "Output while unfocused");
        io.Focused = true;
        await Task.Delay(80);
        Check(io.Events.Count == count, "Focus return must not resume held/toggled lane");
        io.Held[s.ComboTrigger] = io.Held[s.MinorTrigger] = false;
        await Task.Delay(40);
        io.Held[s.ComboTrigger] = true;
        await Until(() => io.Events.Count > count, "fresh trigger resumes");
        Pass(mode + " focus loss releases both lanes and requires fresh trigger");
    }, s => { s.RunMode = mode; s.MinorPedalKey = "0"; s.Timings.SkillKeyHold = 500; });
}

foreach (var (preset, expected) in new[]
{
    ("rr", new[] { "F6", "R", "R" }),
    ("r", new[] { "F6", "R" }),
    ("staff-r", new[] { "F6", "R" }),
    ("bp-rr", new[] { "F6", "R", "R" }),
    ("assassin-rr", new[] { "3", "R", "R" }),
    ("assassin-skill-r", new[] { "3", "R" }),
    ("assassin-r-skill", new[] { "R", "3" })
})
{
    await WithEngine(async (engine, io, s) =>
    {
        io.Held[s.ComboTrigger] = true;
        await Until(() => io.Events.Count(e => e.Down) >= expected.Length, preset);
        Check(io.Events.Where(e => e.Down).Take(expected.Length).Select(e => e.Key).SequenceEqual(expected), preset + " order");
        io.Held[s.ComboTrigger] = false;
        await Until(() => !engine.ComboActive && io.Down.IsEmpty, preset + " release");
        int count = io.Events.Count;
        await Task.Delay(80);
        Check(io.Events.Count == count, preset + " continued after release");
        Pass(preset + " sequence and release");
    }, s => { s.ComboPreset = preset; s.ComboKey1 = "F6"; s.ComboSpeedMs = 50; });
}
await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("S") >= 2, "custom archer cycle");
    Check(io.Events.Where(e => e.Down).Take(4).Select(e => e.Key)
        .SequenceEqual(new[] { "F6", "S", "F8", "S" }), "Three-arrow-first custom bindings");
    Check(io.Count("3") == 0 && io.Count("5") == 0 && io.Count("R") == 0, "No implicit outputs with overrides");
    Pass("Archer three-first order, custom F keys and S slide");
}, s => { s.ComboPreset = "3-w-5-w"; s.ComboKey1 = "F6"; s.ComboKey2 = "F8"; s.SlideKey = "S"; s.ComboSpeedMs = 50; });
Invalid(s => { s.ComboPreset = "3-w-5-w"; s.ComboTrigger = "3"; s.Skills.Clear(); s.FillerOrder.Clear(); });
Invalid(s => s.ComboPreset = "staff-r");
Invalid(s => s.ComboPreset = "bp-rr");
Pass("Staff and BP require an explicit attack key");

await WithEngine(async (engine, io, s) =>
{
    Check(io.Events.IsEmpty && engine.IsArmed, "Arm must not send input");
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 2, "combo sequence");
    var first = io.Events.Where(e => e.Down).Take(3).Select(e => e.Key).ToArray();
    Check(first.SequenceEqual(new[] { "3", "W", "R" }), "Skill W R order");
    Check(io.Count("2") == 0 && io.Count("F1") == 0, "No unsolicited Minor/pages");
    io.Held[s.ComboTrigger] = false;
    await Until(() => !engine.ComboActive && io.Down.IsEmpty, "release combo");
    int count = io.Events.Count;
    await Task.Delay(100);
    Check(io.Events.Count == count, "Input continued after release");
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 3, "resume");
    Check(io.Count("3") == 1, "Release must preserve Spike cooldown");
    Pass("Arm idle; held Skill-W-R; release; cooldown preserved across re-press");
});

await WithEngine(async (engine, io, s) =>
{
    Check(io.Events.IsEmpty, "Held key at arm must not start");
    io.Held[s.ComboTrigger] = false; await Task.Delay(35);
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("3") == 1, "fresh press after preheld");
    Pass("Pre-held trigger requires release and fresh press");
}, preheld: true);

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("3") >= 1, "combo starts on trigger");
    io.Held[s.ComboTrigger] = false;
    await Until(() => !engine.ComboActive && io.Down.IsEmpty, "release combo");
    Pass("Target foreground allows a fresh physical trigger");
});

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = io.Held[s.MinorTrigger] = true;
    await Until(() => io.Count("2") >= 3 && io.Count("R") >= 2, "independent concurrent lanes");
    io.Held[s.ComboTrigger] = false;
    await Until(() => !engine.ComboActive, "combo release");
    int minor = io.Count("2"), combo = io.Count("R");
    await Until(() => io.Count("2") >= minor + 2, "Minor continues alone");
    Check(io.Count("R") == combo, "Combo continued after release");
    io.Held[s.MinorTrigger] = false;
    await Until(() => !engine.MinorActive && io.Down.IsEmpty, "Minor release");
    Pass("Independent Minor/combo; stopping combo does not interrupt Minor");
});

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.MinorTrigger] = true;
    await Until(() => io.Count("2") >= 2, "Minor only");
    Check(io.Count("W") == 0 && io.Count("3") == 0, "Minor started combo");
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 1, "combo starts beside Minor");
    io.Held[s.MinorTrigger] = false;
    await Until(() => !engine.MinorActive, "Minor stops separately");
    int minor = io.Count("2"), combo = io.Count("R");
    await Until(() => io.Count("R") > combo, "combo continues alone");
    Check(io.Count("2") == minor, "Minor continues after release");
    Pass("Minor alone; stopping Minor does not interrupt combo");
});

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Down.ContainsKey("3"), "long skill hold");
    io.Held[s.MinorTrigger] = true;
    await Until(() => io.Count("2") >= 2, "Minor during long skill hold");
    io.Held[s.Hotkeys.EmergencyStop] = true;
    await engine.Completion.WaitAsync(TimeSpan.FromSeconds(1));
    Check(!engine.IsArmed && io.Down.IsEmpty, "Emergency must release every key");
    Pass("Long hold does not block Minor or emergency stop");
}, s => s.Timings.SkillKeyHold = 2000);

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Down.ContainsKey("3"), "long hold before release");
    io.Held[s.ComboTrigger] = false;
    await Until(() => !engine.ComboActive && io.Down.IsEmpty, "release interrupts long hold");
    Check(io.Count("W") == 0, "Sequence continued after release");
    Pass("Release cancels an in-flight 2000ms hold");
}, s => s.Timings.SkillKeyHold = 2000);

await WithEngine(async (engine, io, s) =>
{
    io.FailNext = true; io.Held[s.ComboTrigger] = true;
    await engine.Completion.WaitAsync(TimeSpan.FromSeconds(2));
    Check(engine.Error == "test input failure" && !engine.IsArmed && io.Down.IsEmpty, "Input failure must stop visibly");
    Pass("Send failure stops both lanes and exposes error without logs");
});

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = io.Held[s.MinorTrigger] = true;
    await Until(() => io.Count("R") > 0, "combo enabled");
    Check(io.Count("2") == 0, "Blank Minor must stay disabled");
    await engine.StopAsync(); await engine.StopAsync();
    int count = io.Events.Count;
    engine.Arm(); await Task.Delay(70);
    Check(io.Events.Count == count, "Re-arm with held triggers must stay idle");
    Pass("Disabled Minor; repeated stop; re-arm stays idle");
}, s => s.MinorPedalKey = "");

Check(InputSender.ApplyJitter(50, 0) == 50, "Zero jitter is identity");
for (int i = 0; i < 40; i++)
{
    int v = InputSender.ApplyJitter(50, 4);
    Check(v >= 46 && v <= 54, "Jitter range");
}
Pass("Jitter stays within configured range");

await WithEngine(async (engine, io, s) =>
{
    Check(io.Events.IsEmpty, "toggle arm idle");
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 1, "toggle on first press");
    io.Held[s.ComboTrigger] = false;
    await Until(() => engine.ComboActive, "latched after release");
    int r = io.Count("R");
    await Until(() => io.Count("R") > r, "toggle continues while released");
    io.Held[s.ComboTrigger] = true;
    await Until(() => !engine.ComboActive, "toggle off");
    int stopped = io.Count("R");
    await Task.Delay(80);
    Check(io.Count("R") == stopped, "toggle off stops input");
    Pass("Toggle latches on first press and off on the next");
}, s => s.RunMode = RunMode.Toggle);

await WithEngine(async (engine, io, s) =>
{
    Check(io.Events.IsEmpty, "preheld toggle idle");
    io.Held[s.ComboTrigger] = false;
    await Task.Delay(40);
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 1, "toggle after release");
    Pass("Toggle ignores a trigger already held at arm");
}, s => s.RunMode = RunMode.Toggle, preheld: true);

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 1, "Z then combo");
    Check(io.Count("Z") >= 1, "Z targeting tap");
    var first = io.Events.Where(e => e.Down).Select(e => e.Key).First();
    Check(first == "Z", "Z leads the cycle");
    Pass("Z-targeting taps before the skill cycle");
}, s => { s.ZKey = "Z"; s.ZInterval = 500; });

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.MinorTrigger] = true;
    await Until(() => io.Count("2") >= 2 && io.Count("F1") >= 2, "minor+pot");
    Check(io.Count("W") == 0, "pot lane must not start combo");
    Pass("Minor-Pot taps without interrupting combo");
}, s => s.MinorPotKey = "F1");

{
    Check(ResourceReader.Parse(ResourceKind.Hp, "HP 43%", 1)?.Percent == 43, "Percent parse");
    Check(ResourceReader.Parse(ResourceKind.Mp, "720/1200", 1)?.Percent == 60, "Ratio parse");
    Check(ResourceReader.Parse(ResourceKind.Hp, "1,234", 1) == null, "Thousands text must not become a ratio");
    Check(ResourceReader.Parse(ResourceKind.Hp, "-10%", 1) == null, "Negative percent rejected");
    Check(ResourceReader.Parse(ResourceKind.Hp, "50.5%", 1) == null, "Decimal percent rejected");
    Check(ResourceReader.Parse(ResourceKind.Hp, "43% 44%", 1) == null, "Ambiguous percents rejected");
    Check(ResourceReader.Parse(ResourceKind.Hp, "150% 20%", 1) == null, "Invalid percent candidate closes parse");
    Check(ResourceReader.Parse(ResourceKind.Hp, "40% 100/100", 1) == null, "Mixed percent and ratio rejected");
    Check(ResourceReader.Parse(ResourceKind.Hp, "120/100", 1) == null, "Impossible ratio rejected");
    Check(ResourceReader.Parse(ResourceKind.Hp, "999/100 20/100", 1) == null, "Invalid ratio candidate closes parse");
    Check(ResourceReader.Parse(ResourceKind.Hp, "abc", 1) == null, "Malformed resource OCR rejected");
    Pass("HP/MP OCR parsing rejects malformed, impossible and ambiguous values");
}

{
    var s = FastSettings();
    s.HealthMana.Hp.Enabled = true;
    s.HealthMana.Hp.Region = new UiRegion { W = 20, H = 12 };
    s.HealthMana.Hp.FallbackKey = "F2";
    s.HealthMana.Hp.ThresholdPercent = 50;
    s.HealthMana.Hp.CooldownMs = 500;
    s.HealthMana.Hp.ReadIntervalMs = 100;
    var io = new FakeInput();
    var reader = new FakeResourceReader();
    reader.Values.Enqueue(new ResourceReading(ResourceKind.Hp, 49, Environment.TickCount64));
    reader.Values.Enqueue(new ResourceReading(ResourceKind.Hp, 49, Environment.TickCount64));
    var engine = new MacroEngine(s, io, null, reader);
    engine.Arm();
    try
    {
        await Until(() => io.Count("F2") == 1, "hp pot");
        await Task.Delay(170);
        Check(io.Count("F2") == 1, "HP pot ignored cooldown");
        Pass("HP automation sends fallback below threshold and respects cooldown");
    }
    finally { await engine.StopAsync().WaitAsync(TimeSpan.FromSeconds(2)); }
}

{
    var s = FastSettings();
    s.ComboPreset = "rr";
    s.SkillLayout = new SkillLayout { VisibleBars = 2, Assignments = [new SkillAssignment { Bar = 2, Slot = 3, SkillId = "Spike" }] };
    s.HealthMana.Hp.Enabled = true;
    s.HealthMana.Hp.Region = new UiRegion { W = 20, H = 12 };
    s.HealthMana.Hp.FallbackKey = "7";
    s.HealthMana.Hp.ThresholdPercent = 50;
    s.HealthMana.Hp.ReadIntervalMs = 100;
    s.Timings.SkillKeyHold = 20;
    var io = new FakeInput();
    var reader = new FakeResourceReader();
    var engine = new MacroEngine(s, io, null, reader);
    engine.Arm();
    try
    {
        io.Held[s.ComboTrigger] = true;
        await Until(() => io.Count("F2") >= 1 && io.Count("3") >= 1, "combo selected F2");
        io.Held[s.ComboTrigger] = false;
        reader.Values.Enqueue(new ResourceReading(ResourceKind.Hp, 10, Environment.TickCount64));
        await Until(() => io.Count("F1") >= 1 && io.Count("7") >= 1, "fallback F1 pot");
        var downs = io.Events.Where(e => e.Down).Select(e => e.Key).ToArray();
        Check(Array.LastIndexOf(downs, "F1") < Array.LastIndexOf(downs, "7"), "Numeric fallback did not select F1 before slot");
        Pass("SkillLayout potion fallback is deterministic F1 even after combo selected another bar");
    }
    finally { await engine.StopAsync().WaitAsync(TimeSpan.FromSeconds(2)); }
}

{
    var s = FastSettings();
    s.ComboPreset = "rr";
    s.SkillLayout = new SkillLayout { VisibleBars = 2, Assignments = [new SkillAssignment { Bar = 2, Slot = 3, SkillId = "Spike" }] };
    s.HealthMana.Hp.Enabled = true;
    s.HealthMana.Hp.Region = new UiRegion { W = 20, H = 12 };
    s.HealthMana.Hp.FallbackKey = "7";
    s.HealthMana.Hp.ThresholdPercent = 50;
    s.HealthMana.Hp.ReadIntervalMs = 100;
    s.Timings.SkillKeyHold = 2000;
    var io = new FakeInput();
    var reader = new FakeResourceReader();
    var engine = new MacroEngine(s, io, null, reader);
    engine.Arm();
    try
    {
        io.Held[s.ComboTrigger] = true;
        await Until(() => io.Count("3") >= 1, "blocked combo key");
        reader.Values.Enqueue(new ResourceReading(ResourceKind.Hp, 10, Environment.TickCount64));
        await Task.Delay(1700);
        io.Held[s.ComboTrigger] = false;
        await Task.Delay(250);
        Check(io.Count("7") == 0 && engine.ResourceStatus.Contains("eski"), "Stale potion reading dispatched after waiting on combo");
        Pass("Potion reading freshness is rechecked while waiting for dispatcher");
    }
    finally { await engine.StopAsync().WaitAsync(TimeSpan.FromSeconds(3)); }
}

{
    var s = FastSettings();
    s.HealthMana.Mp.Enabled = true;
    s.HealthMana.Mp.Region = new UiRegion { W = 20, H = 12 };
    s.HealthMana.Mp.ThresholdPercent = 80;
    s.HealthMana.Mp.ReadIntervalMs = 100;
    var io = new FakeInput();
    var reader = new FakeResourceReader();
    reader.Values.Enqueue(new ResourceReading(ResourceKind.Mp, 20, Environment.TickCount64));
    var engine = new MacroEngine(s, io, null, reader);
    engine.Arm();
    try
    {
        await Until(() => engine.ResourceStatus.Contains("tuşu bulunamadı"), "missing mp binding status");
        Check(io.Events.IsEmpty && engine.ResourceStatus.Contains("tuşu bulunamadı"), "Missing MP binding sent input");
        Pass("MP automation reports missing binding and sends nothing");
    }
    finally { await engine.StopAsync().WaitAsync(TimeSpan.FromSeconds(2)); }
}

{
    var s = FastSettings();
    s.HealthMana.Hp.Enabled = true;
    s.HealthMana.Hp.Region = new UiRegion { W = 20, H = 12 };
    s.HealthMana.Hp.FallbackKey = "F2";
    s.HealthMana.Hp.ThresholdPercent = 50;
    s.HealthMana.Hp.ReadIntervalMs = 100;
    var io = new FakeInput { Focused = false };
    var reader = new FakeResourceReader();
    reader.Values.Enqueue(new ResourceReading(ResourceKind.Hp, 10, Environment.TickCount64));
    var engine = new MacroEngine(s, io, null, reader);
    engine.Arm();
    try
    {
        await Task.Delay(150);
        Check(io.Events.IsEmpty && reader.ReadCount == 0, "Unfocused HP lane read or sent input");
        io.Focused = true;
        await Until(() => io.Count("F2") == 1, "hp pot after focus");
        Pass("HP automation pauses on focus loss and resumes without stale input");
    }
    finally { await engine.StopAsync().WaitAsync(TimeSpan.FromSeconds(2)); }
}

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("R") >= 1, "skill-R");
    var downs = io.Events.Where(e => e.Down).Select(e => e.Key).ToList();
    Check(downs[0] == "3" && downs[1] == "R", "Skill then R");
    Check(!downs.Contains("W"), "Skill-R skips W");
    Pass("Skill-R mode skips slide");
}, s => s.SkillRSkillMode = SkillRSkillMode.SkillR);

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("F1") >= 1 && io.Count("F2") >= 1, "5-3 sequence");
    var downs = io.Events.Where(e => e.Down).Select(e => e.Key).ToList();
    Check(downs[0] == "F1" && downs[1] == "F2", "5 then 3 keys");
    Check(!downs.Contains("W") && !downs.Contains("R"), "sequence skips WR");
    Pass("Archer 5-3 sequence taps bound keys");
}, s => { s.ComboPreset = "5-3"; s.ComboKey1 = "F1"; s.ComboKey2 = "F2"; s.ComboSpeedMs = 50; });

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    io.Held["F4"] = true;
    await Until(() => io.Count("F5") >= 1 && io.Count("3") >= 1, "insert then skill");
    var downs = io.Events.Where(e => e.Down).Select(e => e.Key).ToList();
    Check(downs[0] == "F5", "insert key first");
    Pass("Insert trigger squeezes Stix/Cure/M20 before combo");
}, s => { s.InsertTrigger = "F4"; s.InsertKey = "F5"; });

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("W") >= 2, "3-5-w cycle");
    var downs = io.Events.Where(e => e.Down).Select(e => e.Key).Take(4).ToArray();
    Check(downs.SequenceEqual(new[] { "5", "W", "3", "W" }), "5-W-3-W order");
    Check(io.Count("R") == 0, "3-5-w skips R");
    Pass("Archer 5-W-3-W sequence");
}, s =>
{
    s.ComboPreset = "3-5-w";
    s.ComboSpeedMs = 50;
    foreach (string k in s.Skills.Keys.ToArray()) s.Skills[k] = "";
    s.Skills["MultipleShot"] = "3";
    s.Skills["ArrowShower"] = "5";
});

{
    var s = FastSettings();
    s.ComboPreset = "3-5-w";
    s.ExclusiveBind("ArrowShower", "5");
    s.ExclusiveBind("MultipleShot", "3");
    s.Validate();
    Check(s.Skills["Spike"] == "" && s.Skills["BloodyBeast"] == "", "3/5 evict assassin binds");
    Check(s.Skills["ArrowShower"] == "5" && s.Skills["MultipleShot"] == "3", "archer slots kept");
    Pass("3-5-w ExclusiveBind clears colliding skill keys");
}

await WithEngine(async (engine, io, s) =>
{
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("3") == 1, "first Spike down");
    io.Held[s.ComboTrigger] = false;
    await Until(() => !engine.ComboActive && io.Down.IsEmpty, "cancel skill hold");
    await Task.Delay(40);
    io.Held[s.ComboTrigger] = true;
    await Until(() => io.Count("4") >= 1 || io.Count("3") >= 2, "next skill after cancellation");
    Check(io.Count("3") == 1, "Cancelled hold must retain cooldown from successful key-down");
    Pass("Cancelled skill hold retains successful send cooldown");
}, s => s.Timings.SkillKeyHold = 500);

foreach (string key in new[] { "invalid", "D3", "XBUTTON1", "" })
{
    bool rejected = false;
    try { InputSender.KeyDown(key); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Invalid output must fail visibly: " + key);
}
Pass("Invalid output never silently disappears");

using (var hooks = new WindowsMacroInput())
{
    hooks.Configure(FastSettings());
    hooks.SetArmed(true);
    var swallow = typeof(WindowsMacroInput).GetMethod("Swallow", BindingFlags.NonPublic | BindingFlags.Instance)!;
    bool Swallow(string key, bool down) => (bool)swallow.Invoke(hooks, new object[] { key, down })!;
    Check(Swallow("XBUTTON1", true), "trigger down swallowed");
    hooks.SetArmed(false);
    Check(Swallow("XBUTTON1", false), "matching up swallowed after stop");
    Check(!Swallow("XBUTTON1", true), "unarmed press passed through");
    hooks.SetArmed(true);
    Check(!Swallow("XBUTTON1", true), "pre-armed repeat must still pass through");
    Check(!Swallow("XBUTTON1", false), "pre-armed down needs a visible up");
    Pass("Trigger swallowing preserves down/up pairs across arm/stop");
}

// Aynı tanılama kuyruğu, tetik filtresi ve motor olayları gerçek dosya üzerinden doğrulanır.
string logDirectory = Path.Combine(Path.GetTempPath(), "ko-diagnostics-test-" + Guid.NewGuid());
System.Text.Json.JsonElement[] ReadEvents(string path) => File.ReadLines(path)
    .Select(line => System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(line)).ToArray();
try
{
    InputDiagnostics.Record("disabled_must_not_write");
    Check(!Directory.Exists(logDirectory), "Disabled diagnostics must not create files");
    string path = InputDiagnostics.Start(logDirectory);
    bool duplicateRejected = false;
    try { InputDiagnostics.Start(logDirectory); } catch (InvalidOperationException) { duplicateRejected = true; }
    Check(duplicateRejected, "Double start must not leak a writer");
    await Task.WhenAll(Enumerable.Range(0, 4).Select(worker => Task.Run(() =>
    {
        for (int i = 0; i < 100; i++) InputDiagnostics.Record("parallel", new { worker, i });
    })));
    await InputDiagnostics.StopAsync();
    var events = ReadEvents(path);
    Check(events[0].GetProperty("kind").GetString() == "session_start", "Header first");
    Check(events.Count(e => e.GetProperty("kind").GetString() == "parallel") == 400, "All producers drained");
    Check(events[^1].GetProperty("kind").GetString() == "session_end" && events[^1].GetProperty("dropped").GetInt32() == 0, "Clean footer");
    Check(events[..^1].Select(e => e.GetProperty("sequence").GetInt32()).Distinct().Count() == 401, "Unique sequence IDs");
    long length = new FileInfo(path).Length;
    InputDiagnostics.Record("after_stop");
    await InputDiagnostics.StopAsync();
    Check(new FileInfo(path).Length == length && !InputDiagnostics.Active, "Stop idempotent; no late writes");
    Pass("Diagnostics opt-in, concurrent producers, JSONL drain and idempotent stop");

    path = InputDiagnostics.Start(logDirectory, maxEvents: 5);
    for (int i = 0; i < 100; i++) InputDiagnostics.Record("bounded", new { i });
    Check(!InputDiagnostics.Active, "Event cap disables logging without blocking input");
    await InputDiagnostics.StopAsync();
    events = ReadEvents(path);
    Check(events.Length == 6 && events[^1].GetProperty("eventLimitReached").GetBoolean(), "Bounded file and honest truncation footer");
    Pass("Diagnostics event limit and clean restart");

    path = InputDiagnostics.Start(logDirectory);
    using (var hooks = new WindowsMacroInput())
    {
        hooks.Configure(FastSettings());
        var trace = typeof(WindowsMacroInput).GetMethod("TraceTrigger", BindingFlags.NonPublic | BindingFlags.Instance)!;
        void Trace(string key, bool down, bool changed) => trace.Invoke(hooks, new object[] { key, down, changed, false });
        Trace("A", true, true);
        Trace("XBUTTON1", true, true);
        Trace("XBUTTON1", true, false);
        Trace("XBUTTON1", false, true);
        Trace("F12", true, true);
    }
    // Kayıt açıkken yalnızca kendi ürettiğimiz girdiler tanılama etiketi taşır.
    object marked = scanFactory.Invoke(null, new object[] { (ushort)0, 0x04u })!;
    object markedUnion = inputType.GetField("u")!.GetValue(marked)!;
    object markedKeyboard = markedUnion.GetType().GetField("ki")!.GetValue(markedUnion)!;
    Check((IntPtr)markedKeyboard.GetType().GetField("dwExtraInfo")!.GetValue(markedKeyboard)! == InputDiagnostics.EventMarker, "Own injection marker");
    await InputDiagnostics.StopAsync();
    var triggers = ReadEvents(path).Where(e => e.GetProperty("kind").GetString() == "physical_trigger").ToArray();
    Check(triggers.Length == 3, "Only configured transitions, no repeats or ordinary typing");
    Check(!triggers.Any(e => e.GetProperty("data").GetProperty("key").GetString() == "A"), "Ordinary text must not be logged");
    Pass("Diagnostics trigger allowlist / transition filter and own-input marker");

    path = InputDiagnostics.Start(logDirectory);
    await WithEngine(async (engine, io, s) =>
    {
        io.Held[s.ComboTrigger] = true;
        await Until(() => io.Count("3") >= 1, "logged engine send");
        io.FailNext = true;
        await Until(() => engine.Completion.IsCompleted, "logged input failure");
        Check(engine.Error == "test input failure", "Original error preserved");
    });
    await InputDiagnostics.StopAsync();
    events = ReadEvents(path);
    foreach (string kind in new[] { "engine_armed", "lane_active", "engine_tap", "engine_error", "lane_inactive", "engine_stopped" })
        Check(events.Any(e => e.GetProperty("kind").GetString() == kind), "Missing engine trace: " + kind);
    Pass("Diagnostics real engine integration: idle/trigger/send/error/stop");

    string blocked = Path.Combine(logDirectory, "not-a-directory");
    File.WriteAllText(blocked, "fixture");
    bool refused = false;
    try { InputDiagnostics.Start(blocked); } catch (IOException) { refused = true; }
    Check(refused && !InputDiagnostics.Active, "Failed log creation must not leave an active session");
    Pass("Diagnostics file creation failure does not arm logging");

    var sessionType = typeof(InputDiagnostics).GetNestedType("Session", BindingFlags.NonPublic)!;
    object failedSession = Activator.CreateInstance(sessionType, new object[] { "fixture-only", new FailingLogWriter(), 10 })!;
    typeof(InputDiagnostics).GetField("_session", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, failedSession);
    InputDiagnostics.Record("writer_failure");
    await Until(() => !InputDiagnostics.Active, "disk write failure disables logger");
    bool writeFailed = false;
    try { await InputDiagnostics.StopAsync(); } catch (IOException) { writeFailed = true; }
    Check(writeFailed && InputDiagnostics.CurrentPath == null, "Write failure must surface on stop and release session");
    Pass("Diagnostics asynchronous disk failure surfaces without blocking producers");
}
finally
{
    await InputDiagnostics.StopAsync();
    Directory.Delete(logDirectory, true);
}

{
    var io = new FakeInput();
    using var cancel = new CancellationTokenSource();
    bool prepared = false;
    try
    {
        await InputProbe.RunAsync(io, "3", 100, 3, _ => cancel.Cancel(), () => prepared = true, cancel.Token);
        throw new Exception("Cancelled countdown completed");
    }
    catch (OperationCanceledException) { }
    Check(!prepared && io.Events.IsEmpty, "Countdown cancellation must not send input");
    Pass("Delayed probe cancellation before send");
}
{
    var io = new FakeInput();
    using var cancel = new CancellationTokenSource();
    Task probe = InputProbe.RunAsync(io, "3", 3000, 0, _ => { }, () => { }, cancel.Token);
    await Until(() => io.Count("3") == 1, "probe key-down");
    cancel.Cancel();
    try { await probe; } catch (OperationCanceledException) { }
    Check(io.Down.IsEmpty && io.Events.Count == 2, "Cancelled probe must release its key");
    Pass("Delayed probe cancellation during hold releases key");
}
{
    var io = new FakeInput();
    await InputProbe.RunAsync(io, "3", 1, 0, _ => { }, () => { }, CancellationToken.None);
    Check(io.Events.ToArray().SequenceEqual(new[] { ("3", true), ("3", false) }), "Exactly one down/up without a held trigger");
    io = new FakeInput();
    try
    {
        await InputProbe.RunAsync(io, "3", 1, 0, _ => { }, () => throw new InvalidOperationException("target rejected"), CancellationToken.None);
    }
    catch (InvalidOperationException ex) when (ex.Message == "target rejected") { }
    Check(io.Events.IsEmpty, "Invalid foreground must prevent sending");
    io.FailNext = true;
    bool failed = false;
    try { await InputProbe.RunAsync(io, "3", 1, 0, _ => { }, () => { }, CancellationToken.None); }
    catch (InvalidOperationException) { failed = true; }
    Check(failed && io.Down.IsEmpty, "Input failure must propagate without a stuck key");
    Pass("Delayed probe single tap, foreground guard and send failure");
}

{
    var s = FastSettings();
    s.ComboPreset = ComboCatalog.FiveSlideThreeSlide;
    s.ComboKey1 = "6"; s.ComboKey2 = "5";
    var build = typeof(ComboCatalog).GetMethod("SlideSteps");
    Check(build != null, "Slide timing plan must have independent holds and post-release waits");
    var steps = (IEnumerable<(string Key, int Hold, int After)>)build!.Invoke(null, new object[] { s })!;
    Check(steps.SequenceEqual(new[] { ("5", 230, 230), ("W", 19, 19), ("6", 230, 230), ("W", 19, 0) }),
        "SteelSeries reference events including zero final gap");
    Pass("SteelSeries reference timing plan");
}

{
    var s = FastSettings();
    s.ComboPreset = ComboCatalog.FiveSlideThreeSlide;
    s.ComboKey1 = "6"; s.ComboKey2 = "5";
    // Genel sürelerin ve jitter'ın hareketli okçuya sızmadığını sınar.
    s.ComboSpeedMs = s.Timings.SkillToWDelay = s.Timings.SkillKeyHold = 2000;
    s.EnableJitter = true; s.JitterRange = 50;
    var io = new FakeInput();
    var engine = new MacroEngine(s, io);
    await engine.RunOnceAsync().WaitAsync(TimeSpan.FromSeconds(3));
    var events = io.TimedEvents.ToArray();
    Check(events.Select(e => (e.Key, e.Down)).SequenceEqual(new[] {
        ("5", true), ("5", false), ("W", true), ("W", false),
        ("6", true), ("6", false), ("W", true), ("W", false) }), "Actual reference down/up events");
    int[] waits = [230, 230, 19, 19, 230, 230, 19];
    for (int i = 0; i < waits.Length; i++)
    {
        long elapsed = events[i + 1].At - events[i].At;
        Check(elapsed >= waits[i] - 2 && elapsed < waits[i] + 150, $"Reference interval {i}: {elapsed} ms");
    }
    Check(engine.Error == null && io.Down.IsEmpty, "Reference single-cycle cleanup");
    Check(ComboCatalog.SlideSteps(ComboTest.QuickSettings(s)).SequenceEqual(ComboCatalog.SlideSteps(s)), "Quick mode must preserve archer timings");
    Pass("Actual SteelSeries down/up timing, independent of generic delays and jitter");
}
{
    var s = FastSettings(); s.ComboPreset = ComboCatalog.FiveSlideThreeSlide;
    s.ComboKey1 = "6"; s.ComboKey2 = "5";
    var io = new FakeInput(); var engine = new MacroEngine(s, io);
    engine.Arm();
    try
    {
        await Task.Delay(40);
        io.Held[s.ComboTrigger] = true;
        await Until(() => io.Count("5") >= 2, "reference repeat while held");
        var events = io.TimedEvents.ToArray();
        Check(events[8].Key == "5" && events[8].Down && events[8].At - events[7].At < 150, "Final W must not add a cycle delay");
        io.Held[s.ComboTrigger] = false;
        await Until(() => !engine.ComboActive && io.Down.IsEmpty, "reference release");
        int count = io.Events.Count; await Task.Delay(100);
        Check(io.Events.Count == count, "Reference kept sending after release");
        Pass("SteelSeries repeat with zero extra cycle wait and immediate release cleanup");
    }
    finally { await engine.StopAsync(); }
}
foreach (string reason in new[] { "cancel", "focus", "emergency", "send-error" })
{
    var s = FastSettings(); s.ComboPreset = ComboCatalog.ThreeSlideFiveSlide;
    s.ComboKey1 = "6"; s.ComboKey2 = "5";
    var io = new FakeInput(); using var cancel = new CancellationTokenSource();
    var engine = new MacroEngine(s, io);
    Task run = engine.RunOnceAsync(cancel.Token);
    await Until(() => io.Count("W") == 1, "reference first slide");
    if (reason == "cancel") cancel.Cancel();
    if (reason == "focus") io.Focused = false;
    if (reason == "emergency") io.Held[s.Hotkeys.EmergencyStop] = true;
    if (reason == "send-error") io.FailNext = true;
    await run.WaitAsync(TimeSpan.FromSeconds(2));
    Check(io.Count("5") == 0 && io.Down.IsEmpty, "Reference unsafe continuation: " + reason);
    Pass("Reference slide interruption: " + reason);
}
{
    Invalid(s => s.Timings.ArcherSkillHold = 0);
    Invalid(s => s.Timings.ArcherSlideHold = 4);
    Invalid(s => s.Timings.ArcherSkillAfter = -1);
    Invalid(s => s.Timings.ArcherSlideAfter = 2001);
    Invalid(s => s.Timings.ArcherCycleAfter = -1);
    var s = FastSettings(); s.EnsureDefaultProfiles(); s.ActiveProfile = s.Profiles[0].Name;
    s.Timings.ArcherSkillHold = 251; s.Timings.ArcherSkillAfter = 261;
    s.Timings.ArcherSlideHold = 21; s.Timings.ArcherSlideAfter = 31; s.Timings.ArcherCycleAfter = 0;
    s.SaveCurrentToProfile();
    var restored = new Settings { Profiles = Settings.Snapshot(s.Profiles) };
    restored.ApplyProfile(s.ActiveProfile);
    Check(ComboCatalog.SlideSteps(restored).SequenceEqual(ComboCatalog.SlideSteps(s)), "Reference profile roundtrip");
    restored.Timings.ArcherSkillAfter = 999;
    Check(s.Profiles[0].Timings?.ArcherSkillAfter == 261, "Shared timing profile reference");
    var legacy = System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"Timings\":{\"SkillKeyHold\":100}}")!;
    Check(legacy.Timings.SkillKeyHold == 100 && legacy.Timings.ArcherSkillHold == 230 && legacy.Timings.ArcherCycleAfter == 0, "Legacy timing defaults");
    Pass("Archer timing validation, profile isolation and old-settings defaults");
}

passed += InventoryVisionTests.Run();
passed += InventoryTooltipTests.Run();
passed += InventoryReviewTests.Run();
passed += JobSkillCatalogTests.Run();
passed += await SkillDispatcherTests.RunAsync();
passed += SkillLayoutTests.Run();
passed += await SkillCalibrationTests.RunAsync();
passed += SkillIconMatcherTests.Run();
passed += await FarmTests.RunAsync();
Console.WriteLine($"{passed} test groups passed. No real Windows input/GUI was exercised.");

sealed class FailingLogWriter : StreamWriter
{
    public FailingLogWriter() : base(new MemoryStream()) { }
    public override Task WriteLineAsync(string? value) => Task.FromException(new IOException("test disk failure"));
}

sealed class FakeInput : IMacroInput
{
    public readonly ConcurrentDictionary<string, bool> Held = new();
    public readonly ConcurrentDictionary<string, bool> Down = new();
    public readonly ConcurrentQueue<(string Key, bool Down)> Events = new();
    public readonly ConcurrentQueue<(string Key, bool Down, long At)> TimedEvents = new();
    public volatile bool FailNext;
    public bool IsHeld(string key) => Held.GetValueOrDefault(key);
    public volatile bool Focused = true;
    public bool IsTargetForeground(string processName) => Focused;
    public int Count(string key) => Events.Count(e => e.Down && e.Key == key);
    public void KeyDown(string key)
    {
        if (FailNext) { FailNext = false; throw new InvalidOperationException("test input failure"); }
        TimedEvents.Enqueue((key, true, Environment.TickCount64));
        Down[key] = true; Events.Enqueue((key, true));
    }
    public void KeyUp(string key)
    {
        if (Down.TryRemove(key, out _))
        {
            TimedEvents.Enqueue((key, false, Environment.TickCount64));
            Events.Enqueue((key, false));
        }
    }
    public void ReleaseAll() { foreach (string key in Down.Keys) KeyUp(key); }
}

sealed class FakeResourceReader : IResourceReader
{
    public readonly ConcurrentQueue<ResourceReading?> Values = new();
    public int ReadCount;
    public Task<ResourceReading?> ReadAsync(ResourceKind kind, CancellationToken token)
    {
        Interlocked.Increment(ref ReadCount);
        if (Values.TryDequeue(out var value)) return Task.FromResult(value);
        return Task.FromResult<ResourceReading?>(null);
    }
}
