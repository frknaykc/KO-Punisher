using System;
using System.Collections.Generic;
using System.Linq;
using KOPunisher;

internal static class JobSkillCatalogTests
{
    public static int Run()
    {
        int passed = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
        var counts = new Dictionary<ClassType, int>
        {
            [ClassType.Warrior] = 58, [ClassType.Priest] = 99,
            [ClassType.BattlePriest] = 99, [ClassType.Mage] = 91,
            [ClassType.Archer] = 44, [ClassType.Assassin] = 38
        };
        foreach (var (job, count) in counts)
        {
            var skills = JobSkillCatalog.ForJob(job);
            Check(skills.Count == count, $"{job}: reviewed snapshot coverage changed");
            Check(skills.All(s => s.Jobs.Contains(job)), "Cross-job entry leaked");
            Check(skills.Select(s => s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == count,
                "Duplicate skill id");
            Check(JobSkillCatalog.IdsForJob(job).SetEquals(skills.Select(s => s.Id)), "ID index differs");
            foreach (var skill in skills)
            {
                Check(skill.Category is "Attack" or "Buff" or "Heal" or "Utility", "Unknown category");
                Check(!string.IsNullOrWhiteSpace(skill.Name), "Empty name");
                Check(Uri.TryCreate(skill.SourceUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https",
                    "Missing HTTPS provenance");
                Check(skill.IconFile == "" || (skill.IconFile.StartsWith("catalog/", StringComparison.Ordinal) &&
                    skill.IconFile.EndsWith(".png", StringComparison.Ordinal) && !skill.IconFile.Contains("..") &&
                    !skill.IconFile.Contains('\\')), "Unsafe icon path");
                Check(JobSkillCatalog.Find(skill.Id.ToLowerInvariant())?.Id == skill.Id, "Find case mismatch");
                Check(skill.Name is not ("Dribble" or "Shoot" or "Absoluteness" or "Matchless"),
                    "Event/passive skill admitted");
            }
        }
        Pass("All six jobs have reviewed coverage, valid metadata and consistent indexes");
        Check(counts.Keys.SelectMany(JobSkillCatalog.ForJob).Select(s => s.Id).Distinct().Count() == 309,
            "Global catalog size changed");
        Check(JobSkillCatalog.IdsForJob(ClassType.Priest).SetEquals(JobSkillCatalog.IdsForJob(ClassType.BattlePriest)),
            "Battle Priest must share Priest's candidate pool");
        Check(JobSkillCatalog.ForJob(ClassType.Priest).All(s => s.Id.StartsWith("Priest_", StringComparison.Ordinal)),
            "Priest IDs must not collide with Rogue");
        Pass("Shared priest pool has isolated IDs");
        foreach (var id in new[] { "MultipleShot", "ArrowShower", "LightingShot", "PowerShot" })
            Check(JobSkillCatalog.IdsForJob(ClassType.Archer).Contains(id) &&
                !JobSkillCatalog.IdsForJob(ClassType.Assassin).Contains(id), "Archer legacy ID/scope lost");
        foreach (var id in new[] { "Spike", "Stab", "BloodyBeast", "VampiricTouch" })
            Check(JobSkillCatalog.IdsForJob(ClassType.Assassin).Contains(id) &&
                !JobSkillCatalog.IdsForJob(ClassType.Archer).Contains(id), "Assassin legacy ID/scope lost");
        Check(JobSkillCatalog.IdsForJob(ClassType.Archer).Contains("minorhealing") &&
            JobSkillCatalog.IdsForJob(ClassType.Assassin).Contains("MinorHealing"), "Shared Rogue heal missing");
        Check(JobSkillCatalog.Find("Priest_MinorHealing")?.Category == "Heal" &&
            JobSkillCatalog.Find("MinorHealing")?.Category == "Heal", "Heal collision");
        Check(JobSkillCatalog.Find("Mage_FrezingDistance")?.IconFile == "", "Missing icon must stay empty");
        Check(JobSkillCatalog.Find("Mage_Stroke")?.IconFile == "catalog/warrior-stroke.png",
            "Cross-tree source icon was guessed instead of preserved");
        Check(JobSkillCatalog.Find("BeastHiding")?.Category == "Attack", "Damage plus invisibility is an attack");
        Check(JobSkillCatalog.Find("LupinEyes") is null && JobSkillCatalog.Find("BloodRain") is null,
            "Unsupported legacy names must not silently alias different source names");
        Pass("Risky source mappings and legacy IDs are preserved without fuzzy aliases");
        Check(JobSkillCatalog.Find("not-a-skill") is null && JobSkillCatalog.Find("") is null &&
            JobSkillCatalog.Find(null!) is null, "Unknown IDs must be safe");
        Check(JobSkillCatalog.ForJob((ClassType)999).Count == 0 &&
            JobSkillCatalog.IdsForJob((ClassType)999).Count == 0, "Unknown job must have no skills");
        Pass("Unknown jobs and IDs are safe");
        var original = JobSkillCatalog.Find("Spike")!;
        original.Jobs[0] = ClassType.Priest;
        Check(JobSkillCatalog.Find("Spike")!.Jobs.SequenceEqual(new[] { ClassType.Assassin }),
            "Returned record mutation poisoned the catalog");
        var list = JobSkillCatalog.ForJob(ClassType.Assassin);
        list.First(s => s.Id == "Spike").Jobs[0] = ClassType.Priest;
        Check(JobSkillCatalog.ForJob(ClassType.Assassin).First(s => s.Id == "Spike").Jobs[0] == ClassType.Assassin,
            "List record mutation poisoned the catalog");
        Pass("Caller mutation cannot corrupt shared catalog state");
        return passed;
    }
}
