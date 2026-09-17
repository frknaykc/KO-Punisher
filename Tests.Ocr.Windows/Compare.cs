using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using KOPunisher;

internal static class Compare
{
    public static async Task RunAsync(string output)
    {
        Directory.CreateDirectory(output);
        string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var rows = new List<object>();
        foreach (var mode in Enum.GetValues<TooltipOcrMode>())
        foreach (string path in Directory.GetFiles(fixtures, "*.png").OrderBy(x => x))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string? expected = name switch { "blessed-elemental" => "Blessed Elemental Scroll", "pathos" => "Attack Aurora (Pathos' Glove)-Limited Edition", "chitin-helmet" => "Priest Chitin Shell Helmet", _ => null };
            string? kind = name switch { "blessed-elemental" => "Regular item", "pathos" => "Cospre", "chitin-helmet" => "Upgrade Item", _ => null };
            if (expected == null) throw new InvalidOperationException("Missing expected fixture result: " + name);
            using var panel = new Bitmap(path);
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var clock = Stopwatch.StartNew();
            var (text, processed) = await TooltipOcrWindows.ReadAsync(panel, cancel.Token, mode);
            using (processed) processed.Save(Path.Combine(output, name + "." + mode + ".png"), ImageFormat.Png);
            var reading = InventoryTooltip.Read(text, text, false);
            bool exact = reading.Readable && reading.Name == expected && reading.Kind == kind && reading.Level == (name == "chitin-helmet" ? 1 : (int?)null);
            var row = new { Fixture = name, Mode = mode.ToString(), Exact = exact, Ms = clock.ElapsedMilliseconds, Reading = reading };
            rows.Add(row);
            Console.WriteLine(JsonSerializer.Serialize(row));
        }
        File.WriteAllText(Path.Combine(output, "comparison.json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }
}
