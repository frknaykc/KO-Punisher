namespace KOPunisher;

// Skill kimliği kalıcıdır; tuş ve F bar adresi yerleşimden türetilir.
public static class SkillCalibration
{
    public static string[] ComboSkills(Settings settings)
    {
        if (settings.SkillLayout == null) return [];
        if (settings.ClassType == ClassType.Archer)
        {
            if (ComboCatalog.IsSlide(settings.ComboPreset) || settings.ComboPreset is "3-5" or "5-3")
                return ["MultipleShot", "ArrowShower"];
        }
        return settings.SkillLayout.Assignments.OrderBy(a => a.Bar).ThenBy(a => a.Slot)
            .Where(a => JobSkillCatalog.Find(a.SkillId)?.Category == "Attack")
            .Select(a => a.SkillId).Distinct(StringComparer.Ordinal).ToArray();
    }

    public static void Apply(Settings settings, bool requireComplete)
    {
        var layout = settings.SkillLayout;
        if (layout == null) return;
        layout.Validate(JobSkillCatalog.IdsForJob(settings.ClassType));
        if (requireComplete && layout.Assignments.Any(a =>
            new[] { settings.ComboTrigger, settings.MinorTrigger, settings.Hotkeys.Start, settings.Hotkeys.Stop,
                settings.Hotkeys.EmergencyStop, settings.InsertTrigger, settings.ForceHpTrigger }
                .Any(k => string.Equals(k, $"F{a.Bar}", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Skill bar F tuşu tetik/başlat/durdur tuşuyla çakışıyor; kısayolu değiştirin.");
        string Key(string id) => layout.Resolve(id)?.Key ?? "";
        settings.Skills = layout.Assignments.Select(a => a.SkillId).Distinct(StringComparer.Ordinal)
            .ToDictionary(id => id, Key, StringComparer.Ordinal);
        var attacks = ComboSkills(settings);
        bool rotation = settings.ComboPreset is "rotation" or "assassin-rr" or "assassin-skill-r" or "assassin-r-skill";
        int required = rotation ? 1 : settings.ComboPreset switch
        {
            "70-72-60" => 3,
            "70-72" or "70-60" or "3-5" or "5-3" => 2,
            _ => ComboCatalog.IsSlide(settings.ComboPreset) ? 2 : 1
        };
        if (requireComplete && (attacks.Length < required || attacks.Take(required).Any(id => layout.Resolve(id) == null)))
            throw new ArgumentException("Seçili kombo için gerekli saldırı skillerini Skill Bar'a yerleştirin.");
        // Slide alanlarının kimliği: 1=üçlü, 2=beşli. Sıra motorda preset ile belirlenir.
        settings.ComboKey1 = attacks.Length > 0 ? Key(attacks[0]) : "";
        settings.ComboKey2 = attacks.Length > 1 ? Key(attacks[1]) : "";
        settings.ComboKey3 = attacks.Length > 2 ? Key(attacks[2]) : "";
        settings.FillerOrder = attacks.Where(id => id != "Spike" && layout.Resolve(id) != null).ToList();
        settings.MinorPedalKey = Key("MinorHealing");
        settings.LightFeetKey = Key("LightFeet");
    }
}
