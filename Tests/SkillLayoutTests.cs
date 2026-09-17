using System.Text.Json;
using KOPunisher;

internal static class SkillLayoutTests
{
    public static int Run()
    {
        int passed = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
        Settings WithLayout(string layout) => JsonSerializer.Deserialize<Settings>(
            "{\"SkillLayout\":" + layout + "}")!;
        const string layoutJson = """
            {"SchemaVersion":1,"VisibleBars":8,"Assignments":[
                {"Bar":2,"Slot":4,"SkillId":"MultipleShot"},
                {"Bar":8,"Slot":10,"SkillId":"ArrowShower"}]}
            """;
        string SavedLayout(Settings s)
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(s));
            Check(json.RootElement.TryGetProperty("SkillLayout", out var layout),
                "Settings silently discarded the saved F1–F8 skill layout");
            return layout.GetRawText();
        }

        var settings = WithLayout(layoutJson);
        using (var json = JsonDocument.Parse(SavedLayout(settings)))
        {
            var slots = json.RootElement.GetProperty("Assignments");
            Check(slots.GetArrayLength() == 2 && slots[1].GetProperty("Bar").GetInt32() == 8 &&
                slots[1].GetProperty("Slot").GetInt32() == 10, "F8/0 address was lost");
        }
        Pass("Skill layout JSON preserves F2/4 and F8/0 addresses");

        settings.Profiles = [new Profile { Name = "A" }, new Profile { Name = "B" }];
        settings.ActiveProfile = "A";
        settings.SaveCurrentToProfile();
        settings.ApplyProfile("B");
        Check(SavedLayout(settings) == "null", "Legacy profile inherited another profile's layout");
        settings.ApplyProfile("A");
        Check(SavedLayout(settings).Contains("MultipleShot"), "Profile lost its layout");
        Pass("Profile saves isolate new skill layouts and clear them for legacy profiles");

        settings.ClassType = ClassType.Archer;
        settings.SwitchJob(ClassType.Warrior);
        Check(SavedLayout(settings) == "null", "New job inherited another job's skill layout");
        settings.SwitchJob(ClassType.Archer);
        Check(SavedLayout(settings).Contains("ArrowShower"), "Job switch lost archer layout");
        Pass("Skill layouts survive job switching without cross-job leakage");

        foreach (string invalid in new[] {
            "{\"SchemaVersion\":2}", "{\"VisibleBars\":9}", "{\"VisibleBars\":0}",
            "{\"Assignments\":null}", "{\"Assignments\":[null]}",
            "{\"Assignments\":[{\"Bar\":2,\"Slot\":1,\"SkillId\":\"Spike\"}]}",
            "{\"Assignments\":[{\"Bar\":1,\"Slot\":0,\"SkillId\":\"Spike\"}]}",
            "{\"Assignments\":[{\"Bar\":1,\"Slot\":11,\"SkillId\":\"Spike\"}]}",
            "{\"Assignments\":[{\"Bar\":1,\"Slot\":1,\"SkillId\":\"\"}]}",
            "{\"Assignments\":[{\"Bar\":1,\"Slot\":1,\"SkillId\":\"Spike\"}," +
                "{\"Bar\":1,\"Slot\":1,\"SkillId\":\"Thrust\"}]}"
        })
        {
            bool rejected = false;
            try { WithLayout(invalid).Validate(); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "Malformed layout accepted: " + invalid);
        }
        Pass("Malformed, duplicate-slot and future-schema layouts fail closed");

        string directory = Path.Combine(Path.GetTempPath(), "ko-layout-tests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "settings.json");
        try
        {
            settings = WithLayout(layoutJson);
            settings.Save(path);
            var loaded = Settings.Load(path);
            Check(SavedLayout(loaded) == SavedLayout(settings), "Disk roundtrip changed skill layout");
            loaded.SwitchJob(ClassType.Mage);
            loaded.ComboKey1 = "";
            loaded.SaveJobDrafts(path);
            var disk = Settings.Load(path);
            disk.ClassType = ClassType.Mage;
            disk.RestoreJobDraft();
            Check(disk.ComboKey1 == "" && SavedLayout(disk) == "null", "Incomplete draft could not be saved");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        Pass("Atomic layout disk roundtrip and incomplete job draft persistence");
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "MultipleShot", "ArrowShower", "MinorHealing" };
        var layoutModel = new SkillLayout { VisibleBars = 8 };
        for (int bar = 1; bar <= 8; bar++)
        for (int slot = 1; slot <= 10; slot++)
            layoutModel.Assign(bar, slot, "MultipleShot", allowed);
        layoutModel.Validate(allowed);
        Check(layoutModel.Assignments.Count == 80 && layoutModel.Resolve("MultipleShot") == new SkillAddress(1, 1),
            "80-slot capacity or duplicate-skill first-slot policy failed");
        Check(new SkillAddress(8, 10).BarKey == "F8" && new SkillAddress(8, 10).Key == "0",
            "Zero slot encoded as 10 rather than 0");
        Check(layoutModel.Resolve("ArrowShower") == null, "Missing skill silently resolved to a fallback");
        Pass("All 80 addresses, zero-key encoding and deterministic duplicate-skill resolution");

        string before = JsonSerializer.Serialize(layoutModel);
        foreach (var (bar, slot, id) in new[] { (1, 1, "Spike"), (9, 1, "MinorHealing"),
            (1, 11, "MinorHealing"), (1, 1, " MultipleShot"), (1, 1, "multipleshot") })
        {
            bool rejected = false;
            try { layoutModel.Assign(bar, slot, id, allowed); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected && JsonSerializer.Serialize(layoutModel) == before,
                "Invalid drop mutated the previous assignment");
        }
        Pass("Wrong-job, unknown and malformed drop rejection is atomic");

        settings = WithLayout(layoutJson);
        settings.Profiles = [new Profile { Name = "A" }];
        settings.ActiveProfile = "A";
        settings.SaveCurrentToProfile();
        settings.SkillLayout!.Clear(2, 4);
        settings.ApplyProfile("A");
        Check(settings.SkillLayout!.Resolve("MultipleShot") == new SkillAddress(2, 4), "Profile shared the live slot list");
        settings.SkillLayout.Assignments[0].SkillId = "MinorHealing";
        settings.ApplyProfile("A");
        Check(settings.SkillLayout!.Resolve("MultipleShot") == new SkillAddress(2, 4), "Profile shared nested assignments");
        var clone = settings.SkillLayout.Clone();
        clone.Clear(8, 10);
        Check(settings.SkillLayout.Resolve("ArrowShower") == new SkillAddress(8, 10), "Layout clone was shallow");
        Pass("Profile and layout snapshots deep-copy both lists and assignments");
        return passed;
    }
}
