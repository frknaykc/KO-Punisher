#if TOOLTIP_IMAGE_TESTS
using System.IO.Compression;
using KOPunisher;

internal static class TooltipTextImageTests
{
    internal static int Run(string fixtures)
    {
        int passed = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); passed++; }
        Check(TooltipTextImage.Foreground(180, 60, 220), "Purple title erased");
        Check(TooltipTextImage.Foreground(40, 220, 75), "Green title erased");
        Check(TooltipTextImage.Foreground(20, 240, 240), "Cyan title erased");
        Check(!TooltipTextImage.Foreground(65, 70, 45), "Translucent background retained");
        Check(TooltipTextImage.CanonicalType(" REGULAR  ITEM ") == "Regular item", "Exact whitespace/case canonicalization");
        Check(TooltipTextImage.CanonicalType("Upqrade Item") == "", "Fuzzy type invented");
        foreach (var (name, width, height) in new[] { ("blessed-elemental", 304, 223), ("pathos", 326, 321), ("chitin-helmet", 304, 342), ("live-spirit", 428, 268) })
        {
            using var file = File.OpenRead(Path.Combine(fixtures, name + ".rgb.gz"));
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var memory = new MemoryStream(); gzip.CopyTo(memory);
            var rgb = memory.ToArray(); var original = (byte[])rgb.Clone();
            var result = TooltipTextImage.Prepare(rgb, width, height);
            Check(rgb.SequenceEqual(original), "Input pixels mutated: " + name);
            Check(result.HeaderKnown, "Source separators missed: " + name);
            Check(result.Regions.Count(x => x.Label == "header") >= (name == "pathos" ? 3 : 2), "Source header/type bands lost: " + name);
            Check(result.Regions.Any(x => x.Label == "body"), "Source body missing: " + name);
            Check(result.Regions.All(x => x.X >= 0 && x.Y >= 0 && x.X + x.Width <= width && x.Y + x.Height <= height), "Crop overflow");
            if (name == "live-spirit")
                Check(result.Regions[0].X <= 60, "Wide tooltip clipped Spirit title");
            if (name == "pathos")
            {
                Check(result.Regions[0].X == 57 && result.Regions[1].X == 58, "Icon exclusion clipped multiline title");
                Check(result.Mask[110 * width + 280] == 0, "Durability bar retained");
                Check(result.Mask[124 * width + 150] == 0, "Gold separator retained");
            }
            Console.WriteLine(name + " regions: " + string.Join("; ", result.Regions.Select(x => x.ToString())));
        }
        foreach (var (title, kind, level) in new[] { ("Example Scroll", "Regular item", (int?)null), ("Example Helmet(+1)", "Upgrade Item", (int?)1), ("Attack Aurora (Pathos' Glove)-\nLimited Edition", "Cospre", (int?)null) })
        {
            var parts = title.Split('\n').Select(x => ("header", x)).Concat(new[] { ("header", kind), ("body", "Damage +2% increase\nDamage +4% increase") });
            string text = TooltipTextImage.Compose(true, parts);
            var parsed = InventoryTooltip.Read(text, text, false);
            Check(parsed.Readable && parsed.Kind == kind && parsed.Level == level, "Ordered parser integration: " + kind);
            Check(text.Contains("+2%") && text.Contains("+4%"), "Percent text altered");
        }
        foreach (bool known in new[] { true, false })
        {
            string text = TooltipTextImage.Compose(known, new[] { (known ? "header" : "unknown", "Example(+1)"), (known ? "header" : "unknown", "Upqrade Item") });
            Check(!InventoryTooltip.Read(text, text, false).Readable, "Unknown became readable");
        }
        string uncertain = TooltipTextImage.Compose(false, new[] { ("unknown", "Example(+1)\nUpgrade Item") });
        Check(!InventoryTooltip.Read(uncertain, uncertain, false).Readable, "Unsegmented exact words became trusted");
        var blank = TooltipTextImage.Prepare(new byte[100 * 100 * 3], 100, 100);
        Check(!blank.HeaderKnown && blank.Regions.Length == 0, "Blank invented header");
        return passed;
    }
}
#endif
