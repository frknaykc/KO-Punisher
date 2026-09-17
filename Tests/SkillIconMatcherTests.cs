using KOPunisher;
using System.Text.Json;

internal static class SkillIconMatcherTests
{
    public static int Run()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var templates = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SkillIcons", "archer.json")))!;
        foreach (string id in new[] { "ArrowShower", "MultipleShot" })
        {
            var sample = templates[id].ToArray();
            Check(SkillIconMatcher.Match(sample, templates).SkillId == id, "Real catalog icon not recognized: " + id);
            for (int i = 0; i < sample.Length; i++) sample[i] = (byte)Math.Clamp(sample[i] + 6, 0, 255);
            Check(SkillIconMatcher.Match(sample, templates).SkillId == id, "Small brightness change rejected: " + id);
        }
        var duplicate = new Dictionary<string, byte[]> { ["A"] = templates["ArrowShower"], ["B"] = templates["ArrowShower"] };
        Check(SkillIconMatcher.Match(templates["ArrowShower"], duplicate).SkillId == null, "Identical icons must not guess an identity");
        Check(SkillIconMatcher.Match(Enumerable.Repeat((byte)255, SkillIconMatcher.Length).ToArray(), templates).SkillId == null, "Unknown image was assigned");
        Check(SkillIconMatcher.Match(new byte[SkillIconMatcher.Length], new Dictionary<string, byte[]>()).SkillId == null, "Empty templates must not recognize");
        var layout = new SkillLayout { VisibleBars = 2 };
        var allowed = JobSkillCatalog.IdsForJob(ClassType.Archer);
        layout.Assign(1, 1, "ArrowShower", allowed);
        layout.Assign(2, 5, "ArrowShower", allowed);
        var matches = Enumerable.Range(0, 10).Select(_ => new SkillIconMatch(null, 1, 0)).ToArray();
        matches[5] = new("MultipleShot", 0, 1);
        var result = SkillIconMatcher.Merge(layout, 2, matches, allowed);
        Check(result.Resolve("ArrowShower") == new SkillAddress(1, 1) && result.Resolve("MultipleShot") == new SkillAddress(2, 6)
            && result.Assignments.Count == 2 && layout.Assignments.Count == 2 && layout.Resolve("MultipleShot") == null,
            "Scan must clear unknown old slots, preserve other bars and not mutate the source");
        bool rejected = false;
        matches[0] = new("Spike", 0, 1);
        try { SkillIconMatcher.Merge(layout, 2, matches, allowed); } catch (ArgumentException) { rejected = true; }
        Check(rejected && layout.Assignments.Count == 2, "Wrong-job scan must fail atomically");
        Console.WriteLine("PASS real Archer icons, brightness, ambiguity, unknowns and atomic scan application");
        return 9;
    }
}
