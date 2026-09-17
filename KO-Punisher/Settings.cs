using System.Text.Json;
using System.Text.Json.Serialization;

namespace KOPunisher;

/// <summary>
/// Uygulama ayarları — settings.json ile eşleşir.
/// </summary>
public class Settings
{
    // Skill → tuş eşleşmeleri (skill adı -> klavye tuşu)
    [JsonPropertyName("Skills")]
    public Dictionary<string, string> Skills { get; set; } = new()
    {
        { "Spike", "3" },
        { "Thrust", "4" },
        { "BloodyBeast", "5" },
        { "Stab", "6" },
        { "Cut", "7" },
        { "Shock", "8" },
        { "Jab", "9" }
    };

    // Filler rotasyon sırası (Spike hariç)
    [JsonPropertyName("FillerOrder")]
    public List<string> FillerOrder { get; set; } = new()
    {
        "Thrust", "BloodyBeast", "Stab", "Cut", "Shock", "Jab"
    };

    // Timing değerleri (milisaniye)
    [JsonPropertyName("Timings")]
    public Timings Timings { get; set; } = new();

    // Cooldown değerleri (milisaniye)
    [JsonPropertyName("Cooldowns")]
    public Cooldowns Cooldowns { get; set; } = new();

    [JsonPropertyName("SkillCooldowns")]
    public Dictionary<string, int> SkillCooldowns { get; set; } = new()
    {
        ["Thrust"] = 11000,
        ["BloodyBeast"] = 11000,
        ["Stab"] = 11000,
        ["Cut"] = 11000,
        ["Shock"] = 11000,
        ["Jab"] = 11000
    };

    // Minor Pedal tuşu (boşsa devre dışı)
    [JsonPropertyName("MinorPedalKey")]
    public string MinorPedalKey { get; set; } = "";

    public string ComboTrigger { get; set; } = "XBUTTON1";

    public string MinorTrigger { get; set; } = "XBUTTON2";

    [JsonPropertyName("TargetProcess")]
    public string TargetProcess { get; set; } = "KnightOnLine";

    public int MinorHoldMs { get; set; } = 25;

    public int MinorRepeatMs { get; set; } = 100;

    [JsonPropertyName("Hotkeys")]
    public Hotkeys Hotkeys { get; set; } = new();

    // Toggle / Hold modu
    [JsonPropertyName("RunMode")]
    public RunMode RunMode { get; set; } = RunMode.Hold;

    // Rastgele gecikme (jitter) — ayarlanabilir zamanlama
    [JsonPropertyName("EnableJitter")]
    public bool EnableJitter { get; set; } = true;

    [JsonPropertyName("JitterRange")]
    public int JitterRange { get; set; } = 4;

    // Z-Targeting (hedef seçimi)
    [JsonPropertyName("ZKey")]
    public string ZKey { get; set; } = "";

    [JsonPropertyName("ZInterval")]
    public int ZInterval { get; set; } = 3000;

    [JsonPropertyName("ZAttackKey")]
    public string ZAttackKey { get; set; } = "1";

    // Minor-Pot-Mana entegrasyonu
    [JsonPropertyName("MinorPotKey")]
    public string MinorPotKey { get; set; } = "";

    [JsonPropertyName("MinorManaKey")]
    public string MinorManaKey { get; set; } = "";

    // Skill-R-Skill döngüsü desteği
    [JsonPropertyName("SkillRSkillMode")]
    public SkillRSkillMode SkillRSkillMode { get; set; } = SkillRSkillMode.None;

    // W-R / S-R slide tuşu
    [JsonPropertyName("SlideKey")]
    public string SlideKey { get; set; } = "W";

    [JsonPropertyName("ClassType")]
    public ClassType ClassType { get; set; } = ClassType.Assassin;

    [JsonPropertyName("ComboPreset")]
    public string ComboPreset { get; set; } = "rotation";

    [JsonPropertyName("ComboSpeedMs")]
    public int ComboSpeedMs { get; set; } = 90;

    [JsonPropertyName("ComboKey1")]
    public string ComboKey1 { get; set; } = "";

    [JsonPropertyName("ComboKey2")]
    public string ComboKey2 { get; set; } = "";

    [JsonPropertyName("ComboKey3")]
    public string ComboKey3 { get; set; } = "";

    [JsonPropertyName("InsertTrigger")]
    public string InsertTrigger { get; set; } = "";

    [JsonPropertyName("InsertKey")]
    public string InsertKey { get; set; } = "";

    [JsonPropertyName("ForceHpTrigger")]
    public string ForceHpTrigger { get; set; } = "";

    [JsonPropertyName("LightFeetKey")]
    public string LightFeetKey { get; set; } = "";

    [JsonPropertyName("WindowOpacity")]
    public int WindowOpacity { get; set; } = 100;

    [JsonPropertyName("OcrSkillBar")]
    public UiRegion OcrSkillBar { get; set; } = new();

    [JsonPropertyName("OcrChat")]
    public UiRegion OcrChat { get; set; } = new();

    [JsonPropertyName("OcrHpMp")]
    public UiRegion OcrHpMp { get; set; } = new();

    [JsonPropertyName("OcrStatus")]
    public UiRegion OcrStatus { get; set; } = new();

    [JsonPropertyName("OcrFailPhrase")]
    public string OcrFailPhrase { get; set; } = "casting failed";

    [JsonPropertyName("OcrTestCycles")]
    public int OcrTestCycles { get; set; } = 3;

    public FarmSettings Farm { get; set; } = new();

    [JsonPropertyName("HealthMana")]
    public HealthManaSettings HealthMana { get; set; } = new();

    // null: eski tuş ayarları; yerleşim kullanıcı kaydedene kadar otomatik etkinleştirilmez.
    public SkillLayout? SkillLayout { get; set; }

    // Multi-profil sistemi
    [JsonPropertyName("Profiles")]
    public List<Profile> Profiles { get; set; } = new();

    [JsonPropertyName("ActiveProfile")]
    public string ActiveProfile { get; set; } = "Default";

    public void ExclusiveBind(string skillId, string key)
    {
        key = (key ?? "").Trim().ToUpperInvariant();
        skillId = skillId ?? "";
        foreach (string name in Skills.Keys.ToArray())
        {
            if (Skills[name].Equals(key, StringComparison.OrdinalIgnoreCase) &&
                !name.Equals(skillId, StringComparison.OrdinalIgnoreCase))
                Skills[name] = "";
        }
        if (skillId.Length > 0 && key.Length > 0)
            Skills[skillId] = key;
    }

    // Profil adı + job kimliği; taslaklar eksik tuş içerebilir, yalnız başlatırken doğrulanır.
    public Dictionary<string, Profile> JobDrafts { get; set; } = new();

    private string JobIdentity(ClassType job) => ActiveProfile + ":" + job;

    public void StoreJobDraft()
    {
        var copy = Snapshot(this);
        copy.Profiles = [new Profile { Name = copy.ActiveProfile }];
        copy.SaveCurrentToProfile();
        JobDrafts ??= new();
        JobDrafts[JobIdentity(ClassType)] = copy.Profiles[0];
    }

    public void RestoreJobDraft()
    {
        if (JobDrafts?.TryGetValue(JobIdentity(ClassType), out var draft) == true)
            Snapshot(draft).ApplyToSettings(this);
    }

    public void SwitchJob(ClassType job)
    {
        if (job == ClassType) return;
        StoreJobDraft();
        if (JobDrafts.TryGetValue(JobIdentity(job), out var draft))
            Snapshot(draft).ApplyToSettings(this);
        else
        {
            var fresh = new Settings { ClassType = job, ActiveProfile = ActiveProfile };
            if (job != ClassType.Assassin && job != ClassType.Warrior)
            {
                fresh.Skills.Clear();
                fresh.FillerOrder.Clear();
                fresh.SkillCooldowns.Clear();
                fresh.ComboPreset = job switch
                {
                    ClassType.Archer => ComboCatalog.FiveSlideThreeSlide,
                    ClassType.Mage => "staff-r",
                    ClassType.BattlePriest => "bp-rr",
                    _ => "rr"
                };
            }
            fresh.StoreJobDraft();
            fresh.JobDrafts[fresh.JobIdentity(job)].ApplyToSettings(this);
        }
    }

    public void SaveJobDrafts(string? path = null)
    {
        StoreJobDraft();
        // Son geçerli ana ayarı koru; tamamlanmamış job taslağı açılışı bozmasın.
        var disk = Load(path);
        disk.JobDrafts = Snapshot(JobDrafts);
        disk.Save(path);
    }

    internal static T Snapshot<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KO-Punisher", "settings.json");

    /// <summary>
    /// Varsayılan profilleri oluşturur.
    /// </summary>
    public void EnsureDefaultProfiles()
    {
        if (Profiles == null || Profiles.Count == 0)
        {
            Profiles = new List<Profile>
            {
                new Profile
                {
                    Name = "Default (Asas)",
                    ClassType = ClassType.Assassin,
                    Skills = new Dictionary<string, string>
                    {
                        ["Spike"] = "3", ["Thrust"] = "4", ["BloodyBeast"] = "5",
                        ["Stab"] = "6", ["Cut"] = "7", ["Shock"] = "8", ["Jab"] = "9"
                    },
                    FillerOrder = new List<string> { "Thrust", "BloodyBeast", "Stab", "Cut", "Shock", "Jab" },
                    SkillCooldowns = new Dictionary<string, int>
                    { ["Thrust"] = 11000, ["BloodyBeast"] = 11000, ["Stab"] = 11000,
                      ["Cut"] = 11000, ["Shock"] = 11000, ["Jab"] = 11000 }
                },
                new Profile
                {
                    Name = "Warrior",
                    ClassType = ClassType.Warrior,
                    Skills = new Dictionary<string, string>
                    { ["Spike"] = "1", ["Thrust"] = "2", ["BloodyBeast"] = "3", ["Stab"] = "", ["Cut"] = "", ["Shock"] = "", ["Jab"] = "" },
                    FillerOrder = new List<string> { "Thrust", "BloodyBeast" },
                    SkillCooldowns = new Dictionary<string, int> { ["Thrust"] = 11000, ["BloodyBeast"] = 11000 }
                },
                new Profile
                {
                    Name = "Mage",
                    ClassType = ClassType.Mage,
                    Skills = new Dictionary<string, string>
                    { ["Spike"] = "1", ["Thrust"] = "2", ["BloodyBeast"] = "3", ["Stab"] = "", ["Cut"] = "", ["Shock"] = "", ["Jab"] = "" },
                    FillerOrder = new List<string> { "Thrust", "BloodyBeast" },
                    SkillCooldowns = new Dictionary<string, int> { ["Thrust"] = 11000, ["BloodyBeast"] = 11000 }
                },
                new Profile
                {
                    Name = "Priest",
                    ClassType = ClassType.Priest,
                    Skills = new Dictionary<string, string>
                    { ["Spike"] = "1", ["Thrust"] = "2", ["BloodyBeast"] = "", ["Stab"] = "", ["Cut"] = "", ["Shock"] = "", ["Jab"] = "" },
                    FillerOrder = new List<string>(),
                    SkillCooldowns = new Dictionary<string, int>()
                }
            };
        }
    }

    /// <summary>
    /// Aktif profili uygular (Skills, FillerOrder, Cooldowns).
    /// </summary>
    public void ApplyProfile(string profileName)
    {
        var profile = Profiles?.FirstOrDefault(p => p.Name == profileName);
        if (profile == null) return;
        ActiveProfile = profile.Name;
        profile.ApplyToSettings(this);
    }

    /// <summary>
    /// Mevcut ayarları aktif profile kaydeder.
    /// </summary>
    public void SaveCurrentToProfile()
    {
        var profile = Profiles?.FirstOrDefault(p => p.Name == ActiveProfile);
        if (profile == null) return;
        profile.ClassType = ClassType;
        profile.ComboPreset = ComboPreset;
        profile.ComboSpeedMs = ComboSpeedMs;
        profile.ComboKey1 = ComboKey1;
        profile.ComboKey2 = ComboKey2;
        profile.ComboKey3 = ComboKey3;
        profile.InsertTrigger = InsertTrigger;
        profile.InsertKey = InsertKey;
        profile.ForceHpTrigger = ForceHpTrigger;
        profile.LightFeetKey = LightFeetKey;
        profile.JitterRange = JitterRange;
        profile.Skills = new Dictionary<string, string>(Skills);
        profile.FillerOrder = new List<string>(FillerOrder);
        profile.SkillCooldowns = new Dictionary<string, int>(SkillCooldowns);
        profile.Timings = Snapshot(Timings);
        profile.Cooldowns = Snapshot(Cooldowns);
        profile.RunMode = RunMode;
        profile.EnableJitter = EnableJitter;
        profile.ZKey = ZKey;
        profile.ZInterval = ZInterval;
        profile.ZAttackKey = ZAttackKey;
        profile.SkillRSkillMode = SkillRSkillMode;
        profile.SlideKey = SlideKey;
        profile.MinorPotKey = MinorPotKey;
        profile.MinorManaKey = MinorManaKey;
        profile.MinorPedalKey = MinorPedalKey;
        profile.MinorHoldMs = MinorHoldMs;
        profile.MinorRepeatMs = MinorRepeatMs;
        profile.ComboTrigger = ComboTrigger;
        profile.MinorTrigger = MinorTrigger;
        profile.Hotkeys = Snapshot(Hotkeys);
        profile.Farm = Snapshot(Farm);
        profile.HealthMana = Snapshot(HealthMana);
        profile.SkillLayout = SkillLayout?.Clone();
    }

    /// <summary>
    /// Yeni profil ekler.
    /// </summary>
    public void AddProfile(string name, ClassType classType)
    {
        EnsureDefaultProfiles();
        var profile = new Profile
        {
            Name = name, ClassType = classType,
            Skills = new Dictionary<string, string>(),
            FillerOrder = new List<string>(),
            SkillCooldowns = new Dictionary<string, int>()
        };
        Profiles.Add(profile);
        ActiveProfile = name;
        SaveCurrentToProfile();
    }

    /// <summary>
    /// Profil siler.
    /// </summary>
    public void RemoveProfile(string name)
    {
        Profiles?.RemoveAll(p => p.Name == name);
        if (ActiveProfile == name && Profiles?.Count > 0)
            ActiveProfile = Profiles[0].Name;
    }

    /// <summary>
    /// Profil listesini döndürür.
    /// </summary>
    public List<string> GetProfileNames() => Profiles?.Select(p => p.Name).ToList() ?? new List<string>();

    public void Validate()
    {
        if (Skills == null || FillerOrder == null || Timings == null || Cooldowns == null ||
            Hotkeys == null || SkillCooldowns == null)
            throw new ArgumentException("Ayar bölümleri boş olamaz.");

        SkillLayout?.ValidateShape();

        if (string.IsNullOrWhiteSpace(TargetProcess))
            throw new ArgumentException("Hedef oyun process adı boş olamaz.");
        ComboTrigger = (ComboTrigger ?? "").Trim().ToUpperInvariant();
        MinorTrigger = (MinorTrigger ?? "").Trim().ToUpperInvariant();
        MinorPedalKey = (MinorPedalKey ?? "").Trim().ToUpperInvariant();
        MinorPotKey = (MinorPotKey ?? "").Trim().ToUpperInvariant();
        MinorManaKey = (MinorManaKey ?? "").Trim().ToUpperInvariant();
        ZKey = (ZKey ?? "").Trim().ToUpperInvariant();
        ZAttackKey = (ZAttackKey ?? "").Trim().ToUpperInvariant();
        SlideKey = string.IsNullOrWhiteSpace(SlideKey) ? "W" : SlideKey.Trim().ToUpperInvariant();
        Hotkeys.EmergencyStop = (Hotkeys.EmergencyStop ?? "").Trim().ToUpperInvariant();
        ComboPreset = string.IsNullOrWhiteSpace(ComboPreset) ? "rotation" : ComboPreset.Trim().ToLowerInvariant();
        ComboKey1 = (ComboKey1 ?? "").Trim().ToUpperInvariant();
        ComboKey2 = (ComboKey2 ?? "").Trim().ToUpperInvariant();
        ComboKey3 = (ComboKey3 ?? "").Trim().ToUpperInvariant();
        InsertTrigger = (InsertTrigger ?? "").Trim().ToUpperInvariant();
        InsertKey = (InsertKey ?? "").Trim().ToUpperInvariant();
        ForceHpTrigger = (ForceHpTrigger ?? "").Trim().ToUpperInvariant();
        LightFeetKey = (LightFeetKey ?? "").Trim().ToUpperInvariant();

        var triggers = new List<string> { ComboTrigger, MinorTrigger, Hotkeys.EmergencyStop };
        if (InsertTrigger.Length > 0) triggers.Add(InsertTrigger);
        if (ForceHpTrigger.Length > 0) triggers.Add(ForceHpTrigger);
        if (triggers.Any(k => !InputSender.TryGetVkCode(k, out _)) || triggers.Distinct().Count() != triggers.Count)
            throw new ArgumentException("Combo, Minor, acil ve insert/HP tetikleri geçerli ve birbirinden farklı olmalı.");

        foreach (string name in Skills.Keys.ToArray())
            Skills[name] = (Skills[name] ?? "").Trim().ToUpperInvariant();

        foreach (int ms in new[] { Timings.ArcherSkillHold, Timings.ArcherSkillAfter,
            Timings.ArcherSlideHold, Timings.ArcherSlideAfter, Timings.ArcherCycleAfter })
            if (ms < 0 || ms > 2000) throw new ArgumentException("Okçu süreleri 0–2000 ms olmalı.");
        if (Timings.ArcherSkillHold < 5 || Timings.ArcherSlideHold < 5)
            throw new ArgumentException("Okçu basışları en az 5 ms olmalı.");

        bool usesRotation = ComboPreset is "rotation" or "assassin-rr" or "assassin-skill-r" or "assassin-r-skill";
        if (SkillLayout == null && usesRotation && (!Skills.TryGetValue("Spike", out string? spike) || string.IsNullOrEmpty(spike)))
            throw new ArgumentException("Spike için bir skill tuşu gerekli.");

        var outputs = Skills.Values.Where(k => !string.IsNullOrEmpty(k)).Concat(new[] { SlideKey, "R" }).ToList();
        if (MinorPedalKey.Length > 0) outputs.Add(MinorPedalKey);
        if (MinorPotKey.Length > 0) outputs.Add(MinorPotKey);
        if (MinorManaKey.Length > 0) outputs.Add(MinorManaKey);
        if (ZKey.Length > 0) outputs.Add(ZKey);
        void AddOut(string k)
        {
            if (k.Length == 0) return;
            if (!outputs.Contains(k, StringComparer.OrdinalIgnoreCase)) outputs.Add(k);
        }
        AddOut(ComboKey1);
        AddOut(ComboKey2);
        AddOut(ComboKey3);
        AddOut(InsertKey);
        AddOut(LightFeetKey);
        HealthMana ??= new();
        HealthMana.Normalize();
        AddOut(HealthMana.Hp.FallbackKey);
        AddOut(HealthMana.Mp.FallbackKey);
        if (ZKey.Length > 0) AddOut(ZAttackKey);
        // 3-5-W presetinin boş override alanları gerçek 3/5 çıkışlarıdır.
        if (ComboPreset is ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w" or ComboCatalog.ThreeSlideFiveSlide)
        {
            AddOut(ComboKey1.Length == 0 ? "3" : ComboKey1);
            AddOut(ComboKey2.Length == 0 ? "5" : ComboKey2);
        }
        if (ComboPreset is "5-3" or "3-5" or "70-72" or "70-60" or "70-72-60")
        {
            if (ComboKey1.Length == 0 || ComboKey2.Length == 0 ||
                (ComboPreset == "70-72-60" && ComboKey3.Length == 0))
                throw new ArgumentException("Seçilen kombo için tüm çıkış tuşlarını ayarlayın.");
        }
        if (ComboPreset is "staff-r" or "bp-rr" && ComboKey1.Length == 0)
            throw new ArgumentException("Staff / BP için saldırı skilli tuşunu ayarlayın.");
        if (ComboPreset is not ("rotation" or "rr" or "r" or ComboCatalog.FiveSlideThreeSlide or "3-5-w-3-5-w" or
            "assassin-rr" or "assassin-skill-r" or "assassin-r-skill" or "staff-r" or "bp-rr" or ComboCatalog.ThreeSlideFiveSlide or
            "5-3" or "3-5" or "70-72" or "70-60" or "70-72-60"))
            throw new ArgumentException("Bilinmeyen kombo seçimi.");

        if (outputs.Any(k => !InputSender.TryGetVkCode(k, out _) || k is "XBUTTON1" or "XBUTTON2" or "SHIFT" or "CTRL" or "ALT" or "ENTER" or "ESCAPE"))
            throw new ArgumentException("Çıkış tuşları harf, rakam, SPACE veya F1–F20 olmalı.");

        if ((SkillLayout == null && outputs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != outputs.Count) ||
            outputs.Intersect(triggers, StringComparer.OrdinalIgnoreCase).Any())
            throw new ArgumentException("Skill, slide, R, Minor, pot/mana, Z ve tetik tuşları çakışmamalı.");

        if (Farm == null) throw new ArgumentException("Farm ayarları boş olamaz.");
        Farm.Validate(outputs.Concat(triggers).Concat(new[] { Hotkeys.Start, Hotkeys.Stop }));
        HealthMana.Validate(outputs.Concat(triggers).Concat(new[] { Hotkeys.Start, Hotkeys.Stop }));

        if (JitterRange < 0 || JitterRange > 50)
            throw new ArgumentException("Jitter aralığı 0–50 ms olmalı.");

        if (ZKey.Length > 0 && (ZInterval < 500 || ZInterval > 30000))
            throw new ArgumentException("Z aralığı 500–30000 ms olmalı.");

        if (ZAttackKey.Length > 0 && !InputSender.TryGetVkCode(ZAttackKey, out _))
            throw new ArgumentException("Farm saldırı tuşu geçersiz.");

        if (FillerOrder.Any(k => k == null || k == "Spike" || !Skills.ContainsKey(k)) || FillerOrder.Distinct().Count() != FillerOrder.Count)
            throw new ArgumentException("Filler sırası Spike hariç, tekrarsız ve tanımlı skillerden oluşmalı.");

        int[] timings = { Timings.SkillKeyHold, Timings.SkillToWDelay, Timings.WKeyHold,
            Timings.WToRDelay, Timings.RKeyHold, Timings.NextSkillDelay };
        if (timings.Any(v => v < 0 || v > 2000) || Timings.SkillKeyHold < 5 ||
            Timings.WKeyHold < 5 || Timings.RKeyHold < 5 || Timings.NextSkillDelay < 5)
            throw new ArgumentException("Basışlar ve tekrar aralığı 5–2000 ms; ara gecikmeler 0–2000 ms olmalı.");

        if (Cooldowns.Spike < 100 || Cooldowns.Spike > 120000 || SkillCooldowns.Values.Any(v => v < 0 || v > 120000))
            throw new ArgumentException("Cooldown 0–120000 ms olmalı (Spike en az 100 ms).");

        if (MinorHoldMs < 5 || MinorHoldMs > 1000 || MinorRepeatMs < MinorHoldMs + 5 || MinorRepeatMs > 5000)
            throw new ArgumentException("Minor basış 5–1000 ms; periyot basıştan en az 5 ms uzun ve en fazla 5000 ms olmalı.");

        if (ComboSpeedMs < 50 || ComboSpeedMs > 2000)
            throw new ArgumentException("Kombo hızı 50–2000 ms olmalı.");

        if (WindowOpacity < 30 || WindowOpacity > 100)
            throw new ArgumentException("Pencere şeffaflığı 30–100 olmalı.");

        JobDrafts ??= new();
        OcrSkillBar ??= new();
        OcrChat ??= new();
        OcrHpMp ??= new();
        OcrStatus ??= new();
        OcrFailPhrase = string.IsNullOrWhiteSpace(OcrFailPhrase) ? "casting failed" : OcrFailPhrase.Trim();
        if (OcrTestCycles < 1 || OcrTestCycles > 10)
            throw new ArgumentException("OCR test turu 1–10 olmalı.");
        foreach (var r in new[] { OcrSkillBar, OcrChat, OcrHpMp, OcrStatus })
        {
            if (r.W == 0 && r.H == 0 && r.X == 0 && r.Y == 0) continue;
            if (!r.IsSet) throw new ArgumentException("OCR kırpma alanı en az 8×8 olmalı.");
        }
    }

    /// <summary>
    /// Ayarları dosyadan yükle. Dosya yoksa varsayılan değerleri kullan.
    /// </summary>
    public static Settings Load(string? path = null)
    {
        string source = path ?? (File.Exists(FilePath) ? FilePath : Path.Combine(AppContext.BaseDirectory, "settings.json"));
        if (File.Exists(source))
        {
            try
            {
                var json = File.ReadAllText(source);
                var settings = JsonSerializer.Deserialize<Settings>(json) ?? throw new JsonException("Ayar dosyası null olamaz.");
                settings.Validate();
                return settings;
            }
            catch (Exception ex) when (ex is IOException or JsonException or ArgumentException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Ayarlar okunamadı: {source} — {ex.Message}", ex);
            }
        }
        return new Settings();
    }

    /// <summary>
    /// Ayarları dosyaya kaydet.
    /// </summary>
    public void Save(string? path = null)
    {
        Validate();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        });
        string destination = Path.GetFullPath(path ?? FilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, json);
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

public class Timings
{
    // Hareketli okçu combosu: basış ve bırakma sonrası süreler birbirinden bağımsız.
    public int ArcherSkillHold { get; set; } = 230;
    public int ArcherSkillAfter { get; set; } = 230;
    public int ArcherSlideHold { get; set; } = 19;
    public int ArcherSlideAfter { get; set; } = 19;
    public int ArcherCycleAfter { get; set; } = 0;

    [JsonPropertyName("SkillKeyHold")]
    public int SkillKeyHold { get; set; } = 50;

    [JsonPropertyName("SkillToWDelay")]
    public int SkillToWDelay { get; set; } = 30;

    [JsonPropertyName("WKeyHold")]
    public int WKeyHold { get; set; } = 30;

    [JsonPropertyName("WToRDelay")]
    public int WToRDelay { get; set; } = 20;

    [JsonPropertyName("RKeyHold")]
    public int RKeyHold { get; set; } = 30;

    [JsonPropertyName("NextSkillDelay")]
    public int NextSkillDelay { get; set; } = 120;

    [JsonPropertyName("RSpamInterval")]
    public int RSpamInterval { get; set; } = 800;

    /// <summary>
    /// Bir combo'nun tahmini süresi (ms). Spike deadline kontrolü için kullanılır.
    /// </summary>
    public int EstimatedComboDuration =>
        SkillKeyHold + SkillToWDelay + WKeyHold + WToRDelay + RKeyHold + NextSkillDelay;
}

public class Cooldowns
{
    [JsonPropertyName("Spike")]
    public int Spike { get; set; } = 11000;
}

public class Hotkeys
{
    [JsonPropertyName("Start")]
    public string Start { get; set; } = "F8";

    [JsonPropertyName("Stop")]
    public string Stop { get; set; } = "F9";

    [JsonPropertyName("EmergencyStop")]
    public string EmergencyStop { get; set; } = "F12";
}

public enum ResourceKind { Hp, Mp }

public sealed class HealthManaSettings
{
    [JsonPropertyName("Hp")]
    public PotionLaneSettings Hp { get; set; } = new() { SkillId = ResourceReader.HpPotionSkillId, ThresholdPercent = 45 };

    [JsonPropertyName("Mp")]
    public PotionLaneSettings Mp { get; set; } = new() { SkillId = ResourceReader.MpPotionSkillId, ThresholdPercent = 35 };

    public bool AnyEnabled => Hp.Enabled || Mp.Enabled;

    public void Normalize()
    {
        Hp ??= new PotionLaneSettings();
        Mp ??= new PotionLaneSettings();
        Hp.SkillId = ResourceReader.HpPotionSkillId;
        Mp.SkillId = ResourceReader.MpPotionSkillId;
        Hp.Normalize();
        Mp.Normalize();
    }

    public void Validate(IEnumerable<string> reserved)
    {
        Normalize();
        Hp.Validate("HP", reserved);
        Mp.Validate("MP", reserved);
        if (Hp.FallbackKey.Length > 0 && Mp.FallbackKey.Length > 0 &&
            Hp.FallbackKey.Equals(Mp.FallbackKey, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("HP ve MP pot fallback tuşları farklı olmalı.");
    }
}

public sealed class PotionLaneSettings
{
    public bool Enabled { get; set; }
    public string SkillId { get; set; } = "";
    public int ThresholdPercent { get; set; } = 45;
    public string FallbackKey { get; set; } = "";
    public UiRegion Region { get; set; } = new();
    public int ClientWidth { get; set; }
    public int ClientHeight { get; set; }
    public int CooldownMs { get; set; } = 1500;
    public int ReadIntervalMs { get; set; } = 350;

    public void Normalize()
    {
        FallbackKey = (FallbackKey ?? "").Trim().ToUpperInvariant();
        Region ??= new();
        CooldownMs = Math.Clamp(CooldownMs, 250, 30000);
        ReadIntervalMs = Math.Clamp(ReadIntervalMs, 100, 5000);
    }

    public void Validate(string name, IEnumerable<string> reserved)
    {
        Normalize();
        if (ThresholdPercent is < 1 or > 99)
            throw new ArgumentException($"{name} pot eşiği 1-99 olmalı.");
        if (CooldownMs is < 250 or > 30000)
            throw new ArgumentException($"{name} pot cooldown 250-30000 ms olmalı.");
        if (ReadIntervalMs is < 100 or > 5000)
            throw new ArgumentException($"{name} okuma aralığı 100-5000 ms olmalı.");
        if (FallbackKey.Length > 0)
        {
            if (!InputSender.TryGetVkCode(FallbackKey, out _) ||
                FallbackKey is "XBUTTON1" or "XBUTTON2" or "SHIFT" or "CTRL" or "ALT" or "ENTER" or "ESCAPE")
                throw new ArgumentException($"{name} pot fallback tuşu geçersiz.");
        }
        if (Region.W == 0 && Region.H == 0 && Region.X == 0 && Region.Y == 0) return;
        if (!Region.IsSet) throw new ArgumentException($"{name} pot OCR bölgesi en az 8x8 olmalı.");
    }
}

// Çalışma modu (Hold / Toggle)
public enum RunMode { Hold, Toggle }

// Skill-R-Skill döngüsü modu
public enum SkillRSkillMode { None, SkillR, RSkill }

// Sınıf tipi (profil için)
public enum ClassType { Assassin, Warrior, Mage, Priest, Archer, BattlePriest }

// Çoklu profil desteği
public class Profile
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("ClassType")]
    public ClassType ClassType { get; set; } = ClassType.Assassin;

    [JsonPropertyName("Skills")]
    public Dictionary<string, string> Skills { get; set; } = new();

    [JsonPropertyName("FillerOrder")]
    public List<string> FillerOrder { get; set; } = new();

    [JsonPropertyName("SkillCooldowns")]
    public Dictionary<string, int> SkillCooldowns { get; set; } = new();

    [JsonPropertyName("Timings")]
    public Timings? Timings { get; set; }

    [JsonPropertyName("Cooldowns")]
    public Cooldowns? Cooldowns { get; set; }

    [JsonPropertyName("RunMode")]
    public RunMode? RunMode { get; set; }

    [JsonPropertyName("EnableJitter")]
    public bool? EnableJitter { get; set; }

    [JsonPropertyName("ZKey")]
    public string? ZKey { get; set; }

    [JsonPropertyName("ZInterval")]
    public int? ZInterval { get; set; }

    [JsonPropertyName("ZAttackKey")]
    public string? ZAttackKey { get; set; }

    [JsonPropertyName("SkillRSkillMode")]
    public SkillRSkillMode? SkillRSkillMode { get; set; }

    [JsonPropertyName("SlideKey")]
    public string? SlideKey { get; set; }

    [JsonPropertyName("MinorPotKey")]
    public string? MinorPotKey { get; set; }

    [JsonPropertyName("MinorManaKey")]
    public string? MinorManaKey { get; set; }

    [JsonPropertyName("MinorPedalKey")]
    public string? MinorPedalKey { get; set; }

    [JsonPropertyName("MinorHoldMs")]
    public int? MinorHoldMs { get; set; }

    [JsonPropertyName("MinorRepeatMs")]
    public int? MinorRepeatMs { get; set; }

    [JsonPropertyName("ComboTrigger")]
    public string? ComboTrigger { get; set; }

    [JsonPropertyName("MinorTrigger")]
    public string? MinorTrigger { get; set; }

    [JsonPropertyName("Hotkeys")]
    public Hotkeys? Hotkeys { get; set; }

    public string? ComboPreset { get; set; }
    public int? ComboSpeedMs { get; set; }
    public string? ComboKey1 { get; set; }
    public string? ComboKey2 { get; set; }
    public string? ComboKey3 { get; set; }
    public string? InsertTrigger { get; set; }
    public string? InsertKey { get; set; }
    public string? ForceHpTrigger { get; set; }
    public string? LightFeetKey { get; set; }
    public int? JitterRange { get; set; }
    public FarmSettings? Farm { get; set; }
    public HealthManaSettings? HealthMana { get; set; }
    public SkillLayout? SkillLayout { get; set; }

    /// <summary>
    /// Profil ayarlarını ana Settings'e uygular.
    /// </summary>
    public void ApplyToSettings(Settings settings)
    {
        settings.ClassType = ClassType;
        settings.SkillLayout = SkillLayout?.Clone();
        settings.Farm = Farm == null ? new FarmSettings() : Settings.Snapshot(Farm);
        settings.HealthMana = HealthMana == null ? new HealthManaSettings() : Settings.Snapshot(HealthMana);
        if (ComboPreset != null) settings.ComboPreset = ComboPreset;
        if (ComboSpeedMs.HasValue) settings.ComboSpeedMs = ComboSpeedMs.Value;
        if (ComboKey1 != null) settings.ComboKey1 = ComboKey1;
        if (ComboKey2 != null) settings.ComboKey2 = ComboKey2;
        if (ComboKey3 != null) settings.ComboKey3 = ComboKey3;
        if (InsertTrigger != null) settings.InsertTrigger = InsertTrigger;
        if (InsertKey != null) settings.InsertKey = InsertKey;
        if (ForceHpTrigger != null) settings.ForceHpTrigger = ForceHpTrigger;
        if (LightFeetKey != null) settings.LightFeetKey = LightFeetKey;
        if (JitterRange.HasValue) settings.JitterRange = JitterRange.Value;

        if (Skills != null) settings.Skills = new Dictionary<string, string>(Skills);
        if (FillerOrder != null) settings.FillerOrder = new List<string>(FillerOrder);
        if (SkillCooldowns != null) settings.SkillCooldowns = new Dictionary<string, int>(SkillCooldowns);
        if (Timings != null) settings.Timings = Settings.Snapshot(Timings);
        if (Cooldowns != null) settings.Cooldowns = Settings.Snapshot(Cooldowns);
        if (RunMode.HasValue) settings.RunMode = RunMode.Value;
        if (EnableJitter.HasValue) settings.EnableJitter = EnableJitter.Value;
        if (ZKey != null) settings.ZKey = ZKey;
        if (ZInterval.HasValue) settings.ZInterval = ZInterval.Value;
        if (ZAttackKey != null) settings.ZAttackKey = ZAttackKey;
        if (SkillRSkillMode.HasValue) settings.SkillRSkillMode = SkillRSkillMode.Value;
        if (SlideKey != null) settings.SlideKey = SlideKey;
        if (MinorPotKey != null) settings.MinorPotKey = MinorPotKey;
        if (MinorManaKey != null) settings.MinorManaKey = MinorManaKey;
        if (MinorPedalKey != null) settings.MinorPedalKey = MinorPedalKey;
        if (MinorHoldMs.HasValue) settings.MinorHoldMs = MinorHoldMs.Value;
        if (MinorRepeatMs.HasValue) settings.MinorRepeatMs = MinorRepeatMs.Value;
        if (ComboTrigger != null) settings.ComboTrigger = ComboTrigger;
        if (MinorTrigger != null) settings.MinorTrigger = MinorTrigger;
        if (Hotkeys != null) settings.Hotkeys = Settings.Snapshot(Hotkeys);
    }
}

public sealed class UiRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
    public bool IsSet => W >= 8 && H >= 8;
    public override string ToString() => IsSet ? $"{X},{Y} {W}x{H}" : "çizilmedi";
}
