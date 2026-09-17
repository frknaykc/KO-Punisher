using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using KOPunisher;

// Only file bitmap + Windows OCR APIs. Never captures a window or sends input.
if (args.Length > 0 && args[0] == "--compare")
{
    await Compare.RunAsync(args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "comparison"));
    return;
}
string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
Console.WriteLine($"{TooltipTextImageTests.Run(fixtures)} preprocessing checks passed.");
string output = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(output);
var cases = args.Length > 1 ? args.Skip(1).ToArray() : Directory.GetFiles(fixtures, "*.png").OrderBy(x => x).ToArray();
int failures = 0;
foreach (string path in cases)
{
    string name = Path.GetFileNameWithoutExtension(path);
    try
    {
        using var panel = new Bitmap(path);
        Color first = panel.GetPixel(0, 0);
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            try { var unexpected = await TooltipOcrWindows.ReadAsync(panel, cancelled.Token); unexpected.Processed.Dispose(); throw new Exception("Cancellation ignored"); }
            catch (OperationCanceledException) { }
        }
        var (text, processed) = await TooltipOcrWindows.ReadAsync(panel, CancellationToken.None);
        using (processed) processed.Save(Path.Combine(output, name + ".processed.png"), ImageFormat.Png);
        if (panel.GetPixel(0, 0) != first) throw new Exception("Caller bitmap changed");
        var reading = InventoryTooltip.Read(text, text, false);
        File.WriteAllText(Path.Combine(output, name + ".txt"), text);
        File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(reading, new JsonSerializerOptions { WriteIndented = true }));
        string? expected = name switch { "blessed-elemental" => "Blessed Elemental Scroll", "pathos" => "Attack Aurora (Pathos' Glove)-Limited Edition", "chitin-helmet" => "Priest Chitin Shell Helmet", "live-spirit" => "Spirit of Genie", _ => null };
        string? kind = name switch { "blessed-elemental" => "Regular item", "pathos" => "Cospre", "chitin-helmet" => "Upgrade Item", "live-spirit" => "Regular item", _ => null };
        string[] expectedBody = name switch
        {
            "blessed-elemental" => ["*Gives an item additional", "damage capabilities", "(Higher Success Rate)*"],
            "chitin-helmet" => ["Defense Ability : 72", "Required Strength : 90", "Item Grade : High Class"],
            "pathos" => ["Cospre option : Damage +2% increase", "Cospre option : Damage to Priest +4% increase", "Cannot be traded or sold"],
            _ => []
        };
        foreach (string line in expectedBody)
            if (!text.Contains(line, StringComparison.Ordinal)) throw new Exception($"{name}: missing body text: {line}");
        bool ok = reading.Readable && (expected == null || reading.Name == expected && reading.Kind == kind && reading.Level == (name == "chitin-helmet" ? 1 : (int?)null));
        if (!ok) failures++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}: {JsonSerializer.Serialize(reading)}");
    }
    catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {name}: {error}"); }
}
Environment.ExitCode = failures == 0 ? 0 : 1;
