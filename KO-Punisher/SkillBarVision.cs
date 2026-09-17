using System.Drawing;
using System.Drawing.Drawing2D;

namespace KOPunisher;

internal static class SkillBarVision
{
    public static IReadOnlyList<SkillIconMatch> Scan(Bitmap image, IReadOnlyList<JobSkillDef> skills)
    {
        bool vertical = image.Height > image.Width;
        double aspect = vertical ? (double)image.Height / image.Width : (double)image.Width / image.Height;
        if (aspect < 8 || aspect > 13)
            throw new ArgumentException("Tek barın 1–0 arasındaki 10 kare slotunu kenarlardan seçin; yazıları ve diğer barları dışarıda bırakın.");
        var templates = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var skill in skills)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "images", skill.IconFile);
            if (!File.Exists(path)) continue;
            using var icon = Image.FromFile(path);
            templates[skill.Id] = Sample(icon, new RectangleF(0, 0, icon.Width, icon.Height));
        }
        if (templates.Count == 0) throw new InvalidOperationException("Skill ikon dosyaları bulunamadı.");
        var matches = new List<SkillIconMatch>();
        for (int i = 0; i < 10; i++)
        {
            float pitch = (vertical ? image.Height : image.Width) / 10f;
            float side = Math.Min(pitch, vertical ? image.Width : image.Height);
            float inset = (pitch - side) / 2;
            var rect = vertical ? new RectangleF(0, i * pitch + inset, side, side)
                : new RectangleF(i * pitch + inset, 0, side, side);
            matches.Add(SkillIconMatcher.Match(Sample(image, rect), templates));
        }
        return matches;
    }

    private static byte[] Sample(Image image, RectangleF rect)
    {
        using var scaled = new Bitmap(SkillIconMatcher.Side, SkillIconMatcher.Side);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.DrawImage(image, new RectangleF(0, 0, scaled.Width, scaled.Height), rect, GraphicsUnit.Pixel);
        }
        var rgb = new byte[SkillIconMatcher.Length];
        for (int y = 0; y < scaled.Height; y++)
        for (int x = 0; x < scaled.Width; x++)
        {
            Color c = scaled.GetPixel(x, y);
            int i = (y * scaled.Width + x) * 3;
            rgb[i] = c.R; rgb[i + 1] = c.G; rgb[i + 2] = c.B;
        }
        return rgb;
    }
}
