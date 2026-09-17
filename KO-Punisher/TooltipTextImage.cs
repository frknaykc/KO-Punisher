namespace KOPunisher;

// Platform-neutral, RGB24 row-major pixels. No capture, input, or OCR dependencies.
internal static class TooltipTextImage
{
    internal sealed record Region(string Label, int X, int Y, int Width, int Height);
    internal sealed record Prepared(int Width, int Height, byte[] Mask, Region[] Regions, bool HeaderKnown);

    internal static bool Foreground(byte r, byte g, byte b)
    {
        int hi = Math.Max(r, Math.Max(g, b)), lo = Math.Min(r, Math.Min(g, b));
        // Luminance alone discards purple/blue names. Keep bright chromatic glyphs too.
        return lo >= 125 || (hi >= 145 && hi - lo >= 55);
    }

    internal static Prepared Prepare(byte[] rgb, int width, int height)
    {
        if (width < 8 || height < 8 || rgb.Length != checked(width * height * 3))
            throw new ArgumentException("Expected an RGB24 tooltip of at least 8x8 pixels.");
        var mask = new byte[width * height];
        var rules = new bool[height];
        for (int y = 0; y < height; y++)
        {
            int gold = 0;
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 3;
                byte r = rgb[i], g = rgb[i + 1], b = rgb[i + 2];
                mask[y * width + x] = Foreground(r, g, b) ? (byte)1 : (byte)0;
                if (x > width / 50 && x < width - width / 50 && r >= 65 && g >= 55 &&
                    r > b * 1.3 && g > b * 1.2 && Math.Abs(r - g) < 85) gold++;
            }
            rules[y] = gold >= width * .60;
            // Remove continuous gold strokes (including the short durability bar),
            // not separated gold letters. Keep the rule detector's original evidence.
            int run = -1;
            for (int x = 0; x <= width; x++)
            {
                int i = (y * width + Math.Min(x, width - 1)) * 3;
                bool goldPixel = x < width && rgb[i] >= 65 && rgb[i + 1] >= 55 &&
                    rgb[i] > rgb[i + 2] * 1.3 && rgb[i + 1] > rgb[i + 2] * 1.2 && Math.Abs(rgb[i] - rgb[i + 1]) < 140;
                if (goldPixel) { if (run < 0) run = x; }
                else if (run >= 0)
                {
                    if (x - run >= Math.Max(20, width / 10)) Array.Clear(mask, y * width + run, x - run);
                    run = -1;
                }
            }
        }
        var separators = new List<int>();
        for (int y = 0; y < height; y++)
        {
            if (!rules[y]) continue;
            if (separators.Count == 0 || y - separators[^1] > 3) separators.Add(y);
            // Only remove detected horizontal rules, not gold glyphs or percent dots.
            Array.Clear(mask, y * width, width);
        }
        int top = separators.FirstOrDefault(y => y < Math.Min(height / 3, width / 5), -1);
        int bottom = top < 0 ? -1 : separators.FirstOrDefault(y => y - top >= width * .18 && y - top <= width * .65, -1);
        bool known = top >= 0 && bottom > top;
        int margin = Math.Max(3, width / 60);
        var regions = new List<Region>();
        if (known)
        {
            // Icon is a square anchored at the header's left. Exclude only its rows,
            // retaining full-width durability/type text underneath it.
            // Panel width grows with description text; icon size does not.
            int layoutWidth = Math.Min(width, (int)Math.Round((bottom - top) * 3.2));
            int iconRight = (int)Math.Round(layoutWidth * .175) + Math.Max(2, layoutWidth / 100);
            int longestFrame = 0;
            for (int y = top + 2; y < Math.Min(bottom, top + layoutWidth / 12); y++)
            {
                int run = 0;
                for (int x = margin; x < layoutWidth / 4; x++)
                {
                    int i = (y * width + x) * 3;
                    run = Math.Min(rgb[i], Math.Min(rgb[i + 1], rgb[i + 2])) >= 125 ? run + 1 : 0;
                    if (run > longestFrame && run >= layoutWidth * .08 && x - run + 1 < layoutWidth * .08)
                    { longestFrame = run; iconRight = x + 3; }
                }
            }
            int iconBottom = top + (int)Math.Round(layoutWidth * .17);
            for (int y = top + 1; y < Math.Min(bottom, iconBottom); y++)
                Array.Clear(mask, y * width, iconRight);
            AddBands("header", top + 3, bottom - 2);
            AddBands("body", bottom + 3, height - margin);
        }
        else AddBands("unknown", margin, height - margin);
        return new(width, height, mask, regions.ToArray(), known);

        void AddBands(string label, int start, int end)
        {
            int first = -1, last = -1;
            for (int y = start; y <= end; y++)
            {
                int count = y == end ? 0 : Enumerable.Range(margin, width - 2 * margin).Count(x => mask[y * width + x] != 0);
                if (count >= 2) { if (first < 0) first = y; last = y; }
                // Scale the component gap with the captured panel, preserving %, i and accents.
                if (first >= 0 && (y - last > Math.Max(2, width / 150) || y == end))
                {
                    if (last - first >= 3)
                    {
                        int left = width, right = 0;
                        for (int yy = first; yy <= last; yy++)
                        for (int x = margin; x < width - margin; x++)
                            if (mask[yy * width + x] != 0) { left = Math.Min(left, x); right = Math.Max(right, x); }
                        if (right > left) regions.Add(new(label, left, first, right - left + 1, last - first + 1));
                    }
                    first = last = -1;
                }
            }
        }
    }

    internal static string CanonicalType(string text)
    {
        string normalized = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        foreach (string kind in new[] { "Regular item", "Upgrade Item", "Cospre" })
            if (normalized.Equals(kind, StringComparison.OrdinalIgnoreCase)) return kind;
        return "";
    }

    internal static string Compose(bool headerKnown, IEnumerable<(string Label, string Text)> recognized)
    {
        var lines = recognized.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToArray();
        var header = lines.Where(x => x.Label == "header").ToArray();
        var kinds = header.Select((x, i) => (Kind: CanonicalType(x.Text), Index: i)).Where(x => x.Kind.Length > 0).ToArray();
        // Unknown segmentation must not accidentally feed the parser an Upgrade header.
        if (!headerKnown || kinds.Length != 1 || kinds[0].Index == 0)
            return "[Unknown tooltip region/type]\n" + string.Join("\n", lines.SelectMany(x => x.Text.Split('\n')).Select(x => "[Unknown] " + x));
        int type = kinds[0].Index;
        return string.Join("\n", header.Take(type).Select(x => x.Text)
            .Concat(new[] { kinds[0].Kind }).Concat(header.Skip(type + 1).Select(x => x.Text))
            .Concat(lines.Where(x => x.Label == "body").Select(x => x.Text)));
    }
}
