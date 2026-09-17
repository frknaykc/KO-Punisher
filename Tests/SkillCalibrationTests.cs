using KOPunisher;

internal static class SkillCalibrationTests
{
    public static async Task<int> RunAsync()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var s = new Settings { ClassType = ClassType.Archer, ComboPreset = "3-5", SkillLayout = new() };
        var ids = JobSkillCatalog.IdsForJob(s.ClassType);
        s.SkillLayout.Assign(1, 5, "ArrowShower", ids);
        s.SkillLayout.Assign(1, 6, "MultipleShot", ids);
        var io = new RecordingInput();
        // Exercise the real engine, not a duplicate of the key resolver.
        s = ComboTest.QuickSettings(s);
        var engine = new MacroEngine(s, io);
        await engine.RunOnceAsync();
        Check(engine.Error == null, engine.Error ?? "");
        Check(engine.TestKeys.Where(k => !k.StartsWith('F')).SequenceEqual(new[] { "6", "5" }), "3-5 must resolve skill identities, not original numeric keys");
        s.SkillLayout!.VisibleBars = 2;
        s.SkillLayout.Clear(1, 5);
        s.SkillLayout.Assign(2, 6, "ArrowShower", ids);
        engine = new MacroEngine(s, io);
        await engine.RunOnceAsync();
        Check(engine.Error == null && engine.TestKeys.SequenceEqual(new[] { "F1", "6", "F2", "6" }), "Same numeric slot on different bars must select both bars");
        s.SkillLayout.Clear(2, 6);
        bool rejected = false;
        try { _ = new MacroEngine(s, io); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Missing required skill must not fall back to legacy key");
        Console.WriteLine("PASS calibrated Archer identity mapping, cross-bar dispatch and missing-skill rejection");
        int passed = 3;
        foreach (var (job, preset) in new[] { (ClassType.Assassin, "rotation"), (ClassType.Priest, "rr"),
            (ClassType.BattlePriest, "bp-rr"), (ClassType.Mage, "staff-r"), (ClassType.Warrior, "rotation") })
        {
            var skill = JobSkillCatalog.ForJob(job).First(x => x.Category == "Attack" && x.Id != "Spike");
            var jobSettings = new Settings { ClassType = job, ComboPreset = preset, SkillLayout = new() { VisibleBars = 2 } };
            jobSettings.SkillLayout.Assign(2, 7, skill.Id, JobSkillCatalog.IdsForJob(job));
            engine = new MacroEngine(ComboTest.QuickSettings(jobSettings), new RecordingInput());
            await engine.RunOnceAsync();
            Check(engine.Error == null && engine.TestKeys.Take(2).SequenceEqual(new[] { "F2", "7" }),
                $"{job}: calibrated primary/rotation did not use assigned skill: {engine.Error}");
            passed++;
        }
        s.SkillLayout.Assign(2, 6, "ArrowShower", ids);
        foreach (string preset in new[] { "5-3", ComboCatalog.FiveSlideThreeSlide, ComboCatalog.ThreeSlideFiveSlide })
        {
            s.ComboPreset = preset;
            s.Timings.ArcherSkillHold = s.Timings.ArcherSlideHold = 5;
            s.Timings.ArcherSkillAfter = s.Timings.ArcherSlideAfter = s.Timings.ArcherCycleAfter = 0;
            engine = new MacroEngine(s, new RecordingInput());
            await engine.RunOnceAsync();
            string first = preset == ComboCatalog.ThreeSlideFiveSlide ? "F1" : "F2";
            Check(engine.Error == null && engine.TestKeys.First() == first, "Preset skill order changed: " + preset);
            passed++;
        }
        s.ComboPreset = "3-5";
        using (var stop = new CancellationTokenSource())
        {
            var cancelInput = new RecordingInput { Down = key => { if (key == "6") stop.Cancel(); } };
            engine = new MacroEngine(s, cancelInput);
            await engine.RunOnceAsync(stop.Token);
            Check(engine.Error != null && engine.TestKeys.SequenceEqual(new[] { "F1", "6" }) && cancelInput.Held.Count == 0,
                "Cancellation must release the held skill and never dispatch the next bar");
            passed++;
        }
        Console.WriteLine("PASS all job primary/rotation mappings, Archer preset order and calibrated engine cancellation");
        return passed;
    }

    private sealed class RecordingInput : IMacroInput
    {
        public bool IsTargetForeground(string process) => true;
        public bool IsHeld(string key) => false;
        public Action<string>? Down { get; init; }
        public HashSet<string> Held { get; } = new();
        public void KeyDown(string key) { Held.Add(key); Down?.Invoke(key); }
        public void KeyUp(string key) { Held.Remove(key); }
        public void ReleaseAll() { Held.Clear(); }
    }
}
