using System.Text.Json.Serialization;

namespace KOPunisher;

// Slot 10 oyundaki 0 tuşudur; kayıt sırası 1–9, 0 olarak kalır.
public sealed record SkillAddress(int Bar, int Slot)
{
    [JsonIgnore] public string BarKey => $"F{Bar}";
    [JsonIgnore] public string Key => Slot == 10 ? "0" : Slot.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public override string ToString() => $"{BarKey} → {Key}";
}

public sealed class SkillAssignment
{
    public int Bar { get; set; } = 1;
    public int Slot { get; set; } = 1;
    public string SkillId { get; set; } = "";
    [JsonIgnore] public SkillAddress Address => new(Bar, Slot);
}

public sealed class SkillLayout
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public int VisibleBars { get; set; } = 1;
    public List<SkillAssignment> Assignments { get; set; } = new();

    public void ValidateShape()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new ArgumentException("Skill path sürümü desteklenmiyor; kayıt değiştirilmedi.");
        if (VisibleBars is < 1 or > 8)
            throw new ArgumentException("Skill bar sayısı 1–8 olmalı.");
        if (Assignments == null || Assignments.Count > 80)
            throw new ArgumentException("Skill path en fazla 80 slot içermeli.");
        var occupied = new HashSet<SkillAddress>();
        foreach (var assignment in Assignments)
        {
            if (assignment == null || assignment.Bar < 1 || assignment.Bar > VisibleBars ||
                assignment.Slot is < 1 or > 10 || string.IsNullOrWhiteSpace(assignment.SkillId) ||
                assignment.SkillId.Length > 128 || assignment.SkillId != assignment.SkillId.Trim())
                throw new ArgumentException("Skill path içinde geçersiz bar, slot veya skill kimliği var.");
            if (!occupied.Add(assignment.Address))
                throw new ArgumentException($"{assignment.Address} slotunda birden fazla skill var.");
        }
    }

    public void Validate(IReadOnlySet<string> allowedSkillIds)
    {
        ValidateShape();
        foreach (var assignment in Assignments)
            if (!allowedSkillIds.Contains(assignment.SkillId))
                throw new ArgumentException($"Bu job için bilinmeyen skill: {assignment.SkillId}");
    }

    // Aynı skill birden çok bardaysa ilk barın ilk slotu kullanılır; diğer yerleşimler silinmez.
    public SkillAddress? Resolve(string skillId)
    {
        ValidateShape();
        return Assignments.Where(a => a.SkillId == skillId)
            .OrderBy(a => a.Bar).ThenBy(a => a.Slot)
            .Select(a => a.Address).FirstOrDefault();
    }

    public void Assign(int bar, int slot, string skillId, IReadOnlySet<string> allowedSkillIds)
    {
        // Başarısız bırakma işlemi mevcut yerleşimi değiştirmemeli.
        var draft = Clone();
        draft.Assignments.RemoveAll(a => a.Bar == bar && a.Slot == slot);
        draft.Assignments.Add(new SkillAssignment { Bar = bar, Slot = slot, SkillId = skillId });
        draft.Validate(allowedSkillIds);
        Assignments = draft.Assignments;
    }

    public void Clear(int bar, int slot) => Assignments.RemoveAll(a => a.Bar == bar && a.Slot == slot);
    public SkillLayout Clone() => Settings.Snapshot(this);
}
