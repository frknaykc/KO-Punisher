using System.Drawing;

namespace KOPunisher;

public sealed record SkillDef(string Id, string DisplayName, string IconFile, int DefaultCooldownMs, bool Rotation);

public static class SkillCatalog
{
    public static IReadOnlyList<SkillDef> Assassin { get; } =
    [
        new("Spike", "Spike", "asas-spike.gif", 11000, false),
        new("Thrust", "Thrust", "asas-thrust.gif", 11000, true),
        new("BloodyBeast", "Bloody Beast", "asas-bloodybeast.gif", 11000, true),
        new("Stab", "Stab", "mykostab.png", 11000, true),
        new("Cut", "Cut", "asas-cut.gif", 11000, true),
        new("Shock", "Shock", "asas-shock.gif", 11000, true),
        new("Jab", "Jab", "asas-jab.gif", 11000, true),
        new("Pierce", "Pierce", "asas-pierce.gif", 11000, true),
        new("Stroke", "Stroke", "mykostroke.png", 11000, true),
        new("BloodRain", "Blood Rain", "asas-bloodrain.gif", 11000, true),
        new("ThrowingKnife", "Throwing Knife", "asas-throwingknife.gif", 11000, true),
        new("VampiricTouch", "Vampiric Touch", "asas-vampirictouch.gif", 11000, true),
        new("CriticalPoint", "Critical Point", "asas-criticalpoint.gif", 0, false),
        new("LightFeet", "Light Feet", "asas-lightfeet.gif", 0, false),
        new("BeastHiding", "Beast Hiding", "asas-beasthiding.gif", 0, false),
        new("Evade", "Evade", "asas-evade.gif", 0, false),
        new("Stealth", "Stealth", "asas-stealth.gif", 0, false),
        new("Illusion", "Illusion", "asas-illusion.gif", 0, false),
        new("Blinding", "Blinding", "asas-blinding.gif", 0, false),
        new("CatsEyes", "Cat's Eyes", "asas-catseyes.gif", 0, false),
        new("LupinEyes", "Lupin Eyes", "asas-lupineyes.gif", 0, false),
        new("MinorHealing", "Minor Healing", "asas-minorhealing.gif", 0, false),
        new("Concentration", "Concentration", "asas-concentration.gif", 0, false),
        new("CureCurse", "Cure Curse", "asas-curecurse.gif", 0, false),
        new("CureDisease", "Cure Disease", "asas-curedisease.gif", 0, false),
        new("Safety", "Safety", "asas-safety.gif", 0, false),
        new("ScaledSkin", "Scaled Skin", "asas-scaledskin.gif", 0, false),
        new("SmokeScreen", "Smoke Screen", "asas-smokescreen.gif", 0, false),
        new("WildAdvent", "Wild Advent", "asas-wildadvent.gif", 0, false)
    ];

    public static IReadOnlyList<SkillDef> Archer { get; } =
    [
        new("ArrowShower", "Arrow Shower", "archer-arrowshower.gif", 0, false),
        new("MultipleShot", "Multiple Shot", "archer-multipleshot.gif", 0, false),
        new("ThroughShot", "Through Shot", "archer-throughshot.gif", 0, false),
        new("PowerShot", "Power Shot", "archer-powershot.gif", 0, false),
        new("PerfectShot", "Perfect Shot", "archer-perfectshot.gif", 0, false),
        new("ArcShot", "Arc Shot", "archer-arcshot.gif", 0, false),
        new("IceShot", "Ice Shot", "archer-iceshot.gif", 0, false),
        new("FireShot", "Fire Shot", "archer-fireshot.gif", 0, false),
        new("FireArrow", "Fire Arrow", "archer-firearrow.gif", 0, false),
        new("PoisonShot", "Poison Shot", "archer-poisonshot.gif", 0, false),
        new("PoisonArrow", "Poison Arrow", "archer-poisonarrow.gif", 0, false),
        new("LightingShot", "Lighting Shot", "archer-lightingshot.gif", 0, false),
        new("ExplosiveShot", "Explosive Shot", "archer-explosiveshot.gif", 0, false),
        new("GuidedArrow", "Guided Arrow", "archer-guidedarrow.gif", 0, false),
        new("BlowArrow", "Blow Arrow", "archer-blowarrow.gif", 0, false),
        new("BlindingStrafe", "Blinding Strafe", "archer-blindingstrafe.gif", 0, false),
        new("CounterStrike", "Counter Strike", "archer-counterstrike.gif", 0, false),
        new("DarkPursuer", "Dark Pursuer", "archer-darkpursuer.gif", 0, false),
        new("ShadowHunter", "Shadow Hunter", "archer-shadowhunter.gif", 0, false),
        new("ShadowShot", "Shadow Shot", "archer-shadowshot.gif", 0, false),
        new("Viper", "Viper", "archer-viper.gif", 0, false)
    ];

    public static string Description(string id) => id switch
    {
        "MultipleShot" => "Üçlü ok", "ArrowShower" => "Beşli ok",
        "MinorHealing" => "Can yenileme", "LightFeet" => "Hız artışı",
        "CureCurse" or "CureDisease" => "Etki temizleme",
        "Stealth" or "BeastHiding" => "Gizlenme",
        "Safety" or "ScaledSkin" => "Savunma artışı",
        _ when Find(id)?.Rotation == true || Archer.Any(s => s.Id == id) || id == "Spike" => "Saldırı skilli",
        _ => "Destek skilli"
    };

    public static IEnumerable<SkillDef> All => Assassin.Concat(Archer);

    public static SkillDef? Find(string id) =>
        All.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static string IconsDirectory
    {
        get
        {
            foreach (string dir in IconSearchDirs())
            {
                if (Directory.Exists(dir)) return dir;
            }
            return Path.Combine(AppContext.BaseDirectory, "images");
        }
    }

    public static Image? LoadIcon(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        foreach (string path in IconCandidates(fileName))
        {
            try { return Image.FromFile(path); }
            catch { }
        }
        return null;
    }

    private static IEnumerable<string> IconSearchDirs()
    {
        string baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "images");

    }

    private static IEnumerable<string> IconCandidates(string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        if (stem.StartsWith("asas-", StringComparison.OrdinalIgnoreCase))
            stem = stem[5..];
        else if (stem.StartsWith("archer-", StringComparison.OrdinalIgnoreCase))
            stem = stem[7..];
        else if (stem.StartsWith("myko", StringComparison.OrdinalIgnoreCase))
            stem = stem[4..];

        string[] names =
        [
            fileName,
            $"asas-{stem}.gif",
            $"archer-{stem}.gif",
            $"{stem}.png",
            $"myko{stem}.png"
        ];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in IconSearchDirs())
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string name in names)
            {
                string path = Path.Combine(dir, name);
                if (seen.Add(path) && File.Exists(path))
                    yield return path;
            }
        }
    }
}
