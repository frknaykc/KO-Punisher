namespace KOPunisher;

// Kimlikler eski profil dosyalarıyla uyum için korunur; görünen ad tek kaynaktır.
public static class ComboCatalog
{
    public const string FiveSlideThreeSlide = "3-5-w";
    public const string ThreeSlideFiveSlide = "3-w-5-w";

    public static bool IsSlide(string id) => id is FiveSlideThreeSlide or ThreeSlideFiveSlide or "3-5-w-3-5-w";

    public static List<(string Key, int Hold, int After)> SlideSteps(Settings settings)
    {
        var t = settings.Timings;
        string three = string.IsNullOrEmpty(settings.ComboKey1) ? "3" : settings.ComboKey1;
        string five = string.IsNullOrEmpty(settings.ComboKey2) ? "5" : settings.ComboKey2;
        string slide = string.IsNullOrEmpty(settings.SlideKey) ? "W" : settings.SlideKey;
        bool threeFirst = settings.ComboPreset == ThreeSlideFiveSlide;
        return [(threeFirst ? three : five, t.ArcherSkillHold, t.ArcherSkillAfter),
            (slide, t.ArcherSlideHold, t.ArcherSlideAfter),
            (threeFirst ? five : three, t.ArcherSkillHold, t.ArcherSkillAfter),
            (slide, t.ArcherSlideHold, t.ArcherCycleAfter)];
    }

    public static string Title(string id) => id switch
    {
        "rotation" => "Skill rotasyonu / Slide",
        "assassin-skill-r" => "Asas: Skill → R",
        "assassin-rr" => "Asas: Skill → R → R",
        "assassin-r-skill" => "Asas: R → Skill",
        "staff-r" => "Mage: Staff → R",
        "bp-rr" => "BP: Skill → R → R",
        "rr" => "Skill → R → R",
        "r" => "Skill → R",
        FiveSlideThreeSlide or "3-5-w-3-5-w" => "5 Slide 3 Slide",
        ThreeSlideFiveSlide => "3 Slide 5 Slide",
        _ => id
    };

    public static string Description(string id) => id switch
    {
        FiveSlideThreeSlide or "3-5-w-3-5-w" => "Beşli hareket",
        ThreeSlideFiveSlide => "Üçlü hareket",
        "3-5" => "Üçlü önce",
        "5-3" => "Beşli önce",
        "70-72" or "70-60" => "İkili atış",
        "70-72-60" => "Üçlü sıra",
        "rotation" => "Skill rotasyonu",
        "staff-r" => "Staff saldırısı",
        "bp-rr" => "BP saldırısı",
        "assassin-rr" or "rr" => "Çift R",
        "assassin-r-skill" => "R önce",
        _ => "Skill saldırısı"
    };
}

public static class ComboTest
{
    // Hızlı gönderim kontrolü; ideal oyun/casting zamanlaması değildir.
    public static Settings QuickSettings(Settings source)
    {
        var test = Settings.Snapshot(source);
        test.EnableJitter = false;
        test.ComboSpeedMs = 100; // Hareketli okçunun bağımsız süreleri değiştirilmez.
        test.Timings.SkillKeyHold = 50;
        test.Timings.WKeyHold = test.Timings.RKeyHold = 30;
        test.Timings.SkillToWDelay = test.Timings.WToRDelay = test.Timings.NextSkillDelay = 100;
        // Yalnız seçili combo; hedef filtresi ve odak/acil durdurma korunur.
        test.ZKey = test.ZAttackKey = test.LightFeetKey = test.InsertKey = "";
        return test;
    }
}
