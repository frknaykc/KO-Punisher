using System.Text.RegularExpressions;

namespace KOPunisher;

public readonly record struct ResourceReading(ResourceKind Kind, int Percent, long CapturedAt);

public interface IResourceReader
{
    Task<ResourceReading?> ReadAsync(ResourceKind kind, CancellationToken token);
}

public static partial class ResourceReader
{
    public const string HpPotionSkillId = "Consumable_HPPotion";
    public const string MpPotionSkillId = "Consumable_MPPotion";

    [GeneratedRegex(@"(?<![\d.])-?\d+(?:[.,]\d+)?\s*%(?![\d.])", RegexOptions.Compiled)]
    private static partial Regex PercentCandidateRegex();

    [GeneratedRegex(@"(?<![\d.,])-?\d+(?:[.,]\d+)?\s*/\s*-?\d+(?:[.,]\d+)?(?![\d.,])", RegexOptions.Compiled)]
    private static partial Regex RatioCandidateRegex();

    public static ResourceReading? Parse(ResourceKind kind, string text, long capturedAt)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var percentCandidates = PercentCandidateRegex().Matches(text).Select(m => m.Value.Trim()).ToArray();
        var ratioCandidates = RatioCandidateRegex().Matches(text).Select(m => m.Value.Trim()).ToArray();
        if (percentCandidates.Length > 0 && ratioCandidates.Length > 0) return null;
        if (percentCandidates.Length > 1 || ratioCandidates.Length > 1) return null;

        if (percentCandidates.Length == 1)
        {
            string raw = percentCandidates[0][..^1].Trim();
            if (!TryPlainInt(raw, out int value) || value is < 0 or > 100) return null;
            return new ResourceReading(kind, value, capturedAt);
        }

        if (ratioCandidates.Length != 1) return null;
        string[] parts = ratioCandidates[0].Split('/');
        if (parts.Length != 2 ||
            !TryPlainInt(parts[0].Trim(), out int current) ||
            !TryPlainInt(parts[1].Trim(), out int max) ||
            max <= 0 || current < 0 || current > max)
            return null;
        int percent = (int)Math.Round(current * 100.0 / max, MidpointRounding.AwayFromZero);
        if (percent is < 0 or > 100) return null;
        return new ResourceReading(kind, percent, capturedAt);
    }

    private static bool TryPlainInt(string text, out int value)
    {
        value = 0;
        if (text.Length == 0 || text.Any(c => !char.IsAsciiDigit(c))) return false;
        return int.TryParse(text, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
