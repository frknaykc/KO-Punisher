namespace KOPunisher;

public sealed record SkillIconMatch(string? SkillId, double Error, double Margin);

// RGB örnekleri; UI, OCR motoru veya Windows bağımlılığı yoktur.
public static class SkillIconMatcher
{
    public const int Side = 24;
    public const int Length = Side * Side * 3;

    public static SkillIconMatch Match(byte[] sample, IReadOnlyDictionary<string, byte[]> templates)
    {
        if (sample.Length != Length || templates.Values.Any(v => v.Length != Length))
            throw new ArgumentException("Skill ikon örneği 24×24 RGB olmalı.");
        var scores = templates.Select(pair => (pair.Key, Error: Distance(sample, pair.Value)))
            .OrderBy(p => p.Error).ToArray();
        if (scores.Length == 0) return new(null, 1, 0);
        double margin = scores.Length > 1 ? scores[1].Error - scores[0].Error : 1;
        // Çok benzer ikonlar ve bilinmeyen görüntüler kullanıcıya bırakılır.
        string? id = scores[0].Error <= 0.15 && margin >= 0.025 ? scores[0].Key : null;
        return new(id, scores[0].Error, margin);
    }

    private static double Distance(byte[] a, byte[] b)
    {
        double sum = 0;
        int count = 0;
        for (int y = 3; y < Side - 3; y++)
        for (int x = 3; x < Side - 3; x++)
        {
            // Sol üst slot numarası ve alt miktar/cooldown yazısı dikkate alınmaz.
            if ((x < 9 && y < 9) || y >= 19) continue;
            for (int c = 0; c < 3; c++)
            {
                int i = (y * Side + x) * 3 + c;
                sum += Math.Abs(a[i] - b[i]);
                count++;
            }
        }
        return sum / (count * 255.0);
    }

    public static SkillLayout Merge(SkillLayout source, int bar, IReadOnlyList<SkillIconMatch> matches,
        IReadOnlySet<string> allowed)
    {
        if (matches.Count != 10 || bar < 1 || bar > source.VisibleBars)
            throw new ArgumentException("Tarama görünür bir barın 10 slotunu içermeli.");
        var result = source.Clone();
        // Taranan barı yenile: bilinmeyen slotta eski bir skill bırakmak yanlış combo üretir.
        for (int slot = 1; slot <= 10; slot++)
        {
            result.Clear(bar, slot);
            if (matches[slot - 1].SkillId is string id) result.Assign(bar, slot, id, allowed);
        }
        result.Validate(allowed);
        return result;
    }
}
