using System.Text.RegularExpressions;

namespace KOPunisher;

public sealed record InventoryItemReading(string Name, int? Level, string RawText, string Status)
{
    public string Kind { get; init; } = "";
    public bool Readable => Status == "Okundu" && !string.IsNullOrWhiteSpace(Name);
    public bool Upgradeable => Readable && Kind == "Upgrade Item" && Level.HasValue;
    public string Identity => Readable ? $"{Kind}\n{Name}\n{Level?.ToString() ?? "—"}" : Status;
}

public static class InventoryTooltip
{
    // İsim/+seviye tahmini veya O→0 gibi düzeltmeler yapılmaz.
    private static readonly Regex Header = new(@"^(?<name>[^+\r\n]{2,100}?)\s*\(\s*\+\s*(?<level>[0-9]{1,2})\s*\)\s*$", RegexOptions.CultureInvariant);
    public static (string Name, int Level)? ParseHeader(string text)
    {
        var match = Header.Match(text.Trim());
        if (!match.Success || !int.TryParse(match.Groups["level"].Value, out int level) || level > 30) return null;
        string name = Regex.Replace(match.Groups["name"].Value.Trim(), @"\s+", " ");
        return (name, level);
    }
    public static InventoryItemReading Read(string first, string second, bool matchesEmptyReference)
    {
        var a = ParsePanel(first); var b = ParsePanel(second);
        if (a != null && b != null && a == b)
            return new(a.Value.Name, a.Value.Level, second, "Okundu") { Kind = a.Value.Kind };
        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(second) && matchesEmptyReference)
            return new("", null, "", "Boş (referans eşleşti)");
        return new("", null, second, "Okunamadı — işlem dışı");
    }
    // Girdi yalnız görsel olarak ayrılmış tooltip'tir. Tür satırından sonraki
    // açıklama, yüzde ve sayılar isim/seviye aramasına katılmaz.
    private static (string Name, int? Level, string Kind)? ParsePanel(string text)
    {
        var lines = text.Split('\n').Select(x => Regex.Replace(x.Trim(), @"\s+", " "))
            .Where(x => x.Length > 0).ToArray();
        if (lines.Length == 0 || Headers(text).Length > 1) return null;
        string Kind(string line) => line.Equals("Upgrade Item", StringComparison.OrdinalIgnoreCase) ? "Upgrade Item" :
            line.Equals("Regular item", StringComparison.OrdinalIgnoreCase) ? "Regular item" :
            line.Equals("Cospre", StringComparison.OrdinalIgnoreCase) ? "Cospre" : "";
        var types = lines.Select((line, i) => (Name: Kind(line), Index: i)).Where(x => x.Name.Length > 0).ToArray();
        if (types.Length > 1) return null;
        if (types.Length == 0)
        {
            // Eski başlık görüntülenebilir ama açık Upgrade Item türü olmadan seçilemez.
            int start = lines[0].Equals("Inventory", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            var legacy = start < lines.Length ? ParseHeader(lines[start]) : null;
            return legacy.HasValue ? (legacy.Value.Name, legacy.Value.Level, "") : null;
        }
        var titles = lines.Take(types[0].Index).ToList();
        if (titles.Count > 0 && titles[0].Equals("Inventory", StringComparison.OrdinalIgnoreCase)) titles.RemoveAt(0);
        if (titles.Count is < 1 or > 3 || titles.Any(x => x.Contains(':') || x.Contains('%') || x.StartsWith('*'))) return null;
        string title = titles.Aggregate("", (s, line) => s.Length == 0 ? line : s + (s.EndsWith('-') ? "" : " ") + line);
        if (title.Length > 140 || title.Count(char.IsLetter) < 2 || title.Contains('=')) return null;
        var header = ParseHeader(title);
        if (Regex.IsMatch(title, @"\(\s*\+") && !header.HasValue) return null;
        if (header.HasValue) return (header.Value.Name, header.Value.Level, types[0].Name);
        return (title, null, types[0].Name);
    }
    private static (string Name, int Level)[] Headers(string text) => text.Split('\n')
        .Select(ParseHeader).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
    public static bool SameItem(InventoryItemReading before, InventoryItemReading after) =>
        before.Readable && after.Readable && before.Name == after.Name && before.Level == after.Level && before.Kind == after.Kind;
    public static bool NeedsUpgrade(InventoryItemReading reading, int target) =>
        target is >= 1 and <= 10 && reading.Upgradeable && reading.Level < target;
    public static string AfterAttempt(InventoryItemReading before, InventoryItemReading after, int target)
    {
        if (!before.Upgradeable || !after.Upgradeable || before.Name != after.Name || before.Kind != after.Kind) return "Dur — kimlik/sonuç belirsiz";
        if (after.Level != before.Level + 1) return "Dur — beklenen seviye artışı doğrulanmadı";
        return after.Level >= target ? "Hedefe ulaştı" : "Sonraki tura uygun";
    }
}
